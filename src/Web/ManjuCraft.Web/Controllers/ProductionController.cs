using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.AI;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ManjuCraft.Web.Controllers;

public class ProductionController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IStoryService _storyService;
    private readonly IEpisodeService _episodeService;
    private readonly IShotService _shotService;

    public ProductionController(
        IProjectService projectService,
        IStoryService storyService,
        IEpisodeService episodeService,
        IShotService shotService)
    {
        _projectService = projectService;
        _storyService = storyService;
        _episodeService = episodeService;
        _shotService = shotService;
    }

    public async Task<IActionResult> Index(long projectId)
    {
        ViewData["Title"] = "漫剧创作";
        ViewBag.HideFooter = true;
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return RedirectToAction("Index", "Projects");

        ViewBag.Project = project;
        return View();
    }

    [HttpPost("extract-shots")]
    public async Task<IActionResult> ExtractShots([FromBody] ShotExtractionRequest req)
    {
        try
        {
            if (req == null || req.ProviderId <= 0 || req.ProjectId <= 0 || req.SelectedAssetIds == null)
                return Json(new { success = false, message = "参数错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            // Get Story for project
            var story = await db.Stories.FirstOrDefaultAsync(s => s.ProjectId == req.ProjectId);
            if (story == null)
                return Json(new { success = false, message = "未找到剧本" });

            // Get chapters ordered by sort order
            var chapters = await db.StoryChapters
                .Where(c => c.StoryId == story.Id)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            if (req.ChapterIdx < 0 || req.ChapterIdx >= chapters.Count)
                return Json(new { success = false, message = "章节索引无效" });

            var chapter = chapters[req.ChapterIdx];

            // Get selected existing assets
            var existingAssets = await db.Assets
                .Where(a => a.ProjectId == req.ProjectId && req.SelectedAssetIds.Contains(a.Id))
                .ToListAsync();

            // Get prompt template
            var templateType = "StoryboardExtraction";
            var template = await db.PromptTemplates
                .Where(p => p.TemplateType == templateType && p.IsDefault)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
            var sysPrompt = template?.Content ?? "你是一个专业的漫剧分镜师。";

            // Build existing assets text
            var assetText = existingAssets.OrderBy(a => a.Order).ThenBy(a => a.Name)
                .Aggregate("", (acc, a) =>
                {
                    var desc = string.IsNullOrEmpty(a.Description) ? "" : $"（{a.Description}）";
                    return acc + "- " + a.Name + desc + "\n";
                });

            var userPrompt = $"章节内容：\n\n{ chapter.Content ?? ""}\n\n现有资产：\n{assetText}\n\n{req.CustomPrompt}";

            // Call AI
            var provider = await db.ApiProviders.FindAsync(req.ProviderId);
            if (provider == null)
                return Json(new { success = false, message = "AI 提供者不存在" });

            var aiService = HttpContext.RequestServices.GetRequiredService<IAiAgentService>();
            var (success, aiResult, errorMsg) = await aiService.ChatAsync(
                req.ProviderId,
                sysPrompt,
                userPrompt,
                CancellationToken.None);

            if (!success || string.IsNullOrEmpty(aiResult))
                return Json(new { success = false, message = errorMsg ?? "AI 调用失败" });

            // Parse AI response
            var cleanJson = ExtractJsonFromResponse(aiResult);
            var parsed = JsonSerializer.Deserialize<StoryboardExtractionResult>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed == null || parsed.shots == null)
                return Json(new { success = false, message = "AI 返回格式解析失败" });

            // Ensure episode exists for this chapter
            var episode = await db.Episodes.FirstOrDefaultAsync(e => e.StoryChapterId == chapter.Id);
            if (episode == null)
            {
                // Create episode from chapter
                episode = new Episode
                {
                    ProjectId = req.ProjectId,
                    StoryChapterId = chapter.Id,
                    Name = chapter.ChapterName ?? $"第{chapter.ChapterNumber}章",
                    Duration = 0,
                    Order = chapter.SortOrder
                };
                db.Episodes.Add(episode);
                await db.SaveChangesAsync();
            }

            // Build a map of existing asset names for checking
            var existingAssetNames = existingAssets.Select(a => a.Name).ToHashSet();

            // Process new assets and collect their names (with role classification)
            var newAssetEntries = new Dictionary<string, long>(); // name -> id
            var allAssetNames = new HashSet<string>(existingAssetNames);

            if (parsed.newAssets != null)
            {
                foreach (var na in parsed.newAssets)
                {
                    if (string.IsNullOrEmpty(na.name)) continue;

                    // Don't create if name already exists
                    if (existingAssetNames.Contains(na.name))
                    {
                        newAssetEntries[na.name] = existingAssets.First(a => a.Name == na.name).Id;
                        continue;
                    }

                    var asset = new Asset
                    {
                        ProjectId = req.ProjectId,
                        AssetType = ParseAssetType(na.assetType),
                        Name = na.name,
                        Description = na.description ?? "",
                        Order = 0
                    };
                    await db.Assets.AddAsync(asset);
                    await db.SaveChangesAsync();
                    newAssetEntries[na.name] = asset.Id;
                    allAssetNames.Add(na.name);
                }
            }

            // Save shots
            var shotCount = 0;
            foreach (var shotData in parsed.shots)
            {
                var shot = new Shot
                {
                    EpisodeId = episode.Id,
                    ShotSize = shotData.shotSize ?? "",
                    CameraMovement = shotData.cameraMovement ?? "",
                    Duration = shotData.duration,
                    Order = shotCount,
                    Description = shotData.shotName ?? ""
                };

                // Build asset refs with role names or existing asset names
                var assetRefs = new List<string>();
                if (shotData.assetRefs != null)
                {
                    foreach (var refName in shotData.assetRefs)
                    {
                        // If it's a known asset name, use it
                        if (allAssetNames.Contains(refName))
                            assetRefs.Add(refName);
                        else
                            assetRefs.Add(refName); // Use what AI returned
                    }
                }
                shot.AssetRefs = string.Join(",", assetRefs);

                db.Shots.Add(shot);
                await db.SaveChangesAsync();

                // Save frames
                if (shotData.frames != null)
                {
                    foreach (var frameData in shotData.frames.OrderBy(f => f.order))
                    {
                        var frame = new ShotFrame
                        {
                            ShotId = shot.Id,
                            ProjectId = req.ProjectId,
                            FrameType = frameData.frameType ?? "Middle",
                            Description = frameData.description ?? "",
                            Order = frameData.order,
                            StartTime = frameData.startTime,
                            Duration = frameData.duration
                        };
                        db.ShotFrames.Add(frame);
                    }
                }

                shotCount++;
            }

            await db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                data = new
                {
                    shotCount = shotCount,
                    assetCount = newAssetEntries.Count > 0 ? newAssetEntries.Count : 0
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "{}";
        var match = System.Text.RegularExpressions.Regex.Match(response, @"```(?:json)?\s*([\s\S]*?)```");
        if (match.Success)
            return match.Groups[1].Value.Trim();
        return response.Trim();
    }

    private static AssetTypeEnum ParseAssetType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return AssetTypeEnum.Actor;
        var t = type.Trim();
        return t switch
        {
            "Actor" => AssetTypeEnum.Actor,
            "Scene" => AssetTypeEnum.Scene,
            "Bgm" => AssetTypeEnum.Bgm,
            "Prop" => AssetTypeEnum.Prop,
            var x when x.StartsWith("Voice", StringComparison.OrdinalIgnoreCase) => AssetTypeEnum.VoiceVoice,
            _ => AssetTypeEnum.Actor
        };
    }
}

public class ShotExtractionRequest
{
    public long ProjectId { get; set; }
    public int ChapterIdx { get; set; }
    public long ProviderId { get; set; }
    public List<long> SelectedAssetIds { get; set; }
    public string CustomPrompt { get; set; }
}

internal class StoryboardExtractionResult
{
    public List<ShotData>? shots { get; set; }
    public List<NewAssetData>? newAssets { get; set; }
}

internal class ShotData
{
    public string? shotName { get; set; }
    public string? shotNumber { get; set; }
    public string? shotSize { get; set; }
    public string? cameraMovement { get; set; }
    public float? duration { get; set; }
    public List<string>? assetRefs { get; set; }
    public List<FrameData>? frames { get; set; }
}

internal class FrameData
{
    public string? frameType { get; set; }
    public string? description { get; set; }
    public int order { get; set; }
    public float? startTime { get; set; }
    public float? duration { get; set; }
}

internal class NewAssetData
{
    public string? assetType { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
}