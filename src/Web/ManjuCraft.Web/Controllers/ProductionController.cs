using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.AI;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

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
            var (success, aiResult, errorMsg, _, _, _) = await aiService.ChatAsync(
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

    #region 提取分镜和资产

    [HttpPost]
    [Route("/api/v1/ai/extract-shots-and-assets")]
    public async Task<IActionResult> ExtractShotsAndAssets([FromBody] ShotAssetExtractionRequest req)
    {
        try
        {
            if (req == null || req.ProviderId <= 0 || req.ProjectId <= 0)
                return Json(new { success = false, message = "参数错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var story = await db.Stories.FirstOrDefaultAsync(s => s.ProjectId == req.ProjectId);
            if (story == null)
                return Json(new { success = false, message = "未找到剧本" });

            var chapters = await db.StoryChapters
                .Where(c => c.StoryId == story.Id)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            if (req.ChapterIdx < 0 || req.ChapterIdx >= chapters.Count)
                return Json(new { success = false, message = "章节索引无效" });

            var chapter = chapters[req.ChapterIdx];

            var existingAssets = await db.Assets
                .Where(a => a.ProjectId == req.ProjectId)
                .ToListAsync();

            var template = await db.PromptTemplates
                .Where(p => p.TemplateType == "ShotAssetExtraction" && p.IsDefault)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
            var sysPrompt = template?.Content ?? "你是一个专业的漫剧分镜师和资产管理员。";

            var assetText = existingAssets.OrderBy(a => a.Order).ThenBy(a => a.Name)
                .Aggregate("", (acc, a) =>
                {
                    var desc = string.IsNullOrEmpty(a.Description) ? "" : $"（{a.Description}）";
                    return acc + $"- {a.Name} [{a.AssetType}]{desc}\n";
                });

            var userPrompt = $"章节内容：\n\n{chapter.Content ?? ""}\n\n现有资产列表：\n{(string.IsNullOrEmpty(assetText) ? "（暂无资产）" : assetText)}\n\n{req.CustomPrompt}";

            var aiService = HttpContext.RequestServices.GetRequiredService<IAiAgentService>();
            var result = await aiService.ChatAsync(
                req.ProviderId,
                sysPrompt,
                userPrompt,
                CancellationToken.None);

            if (!result.success)
                return Json(new { success = false, message = result.message ?? "AI 调用失败" });

            if (result.isComfyui)
            {
                return Json(new
                {
                    success = true,
                    isComfyui = true,
                    promptId = result.promptId,
                    workflowType = result.workflowType,
                    chapterIdx = req.ChapterIdx,
                    projectId = req.ProjectId,
                    data = new { }
                });
            }

            return Json(new { success = true, isComfyui = false, rawResponse = result.data!, data = ParseAiResult(result.data!, existingAssets) });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/ai/parse-extraction-result")]
    public async Task<IActionResult> ParseExtractionResult([FromBody] SaveExtractionRequest req)
    {
        try
        {
            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();
            var existingAssets = await db.Assets
                .Where(a => a.ProjectId == req.ProjectId)
                .ToListAsync();

            return Json(new { success = true, data = ParseAiResult(req.AiResponse, existingAssets) });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private object ParseAiResult(string aiResponse, List<Asset> existingAssets)
    {
        var cleanJson = ExtractJsonFromResponse(aiResponse);
        var parsed = JsonSerializer.Deserialize<ShotAssetExtractionResult>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (parsed == null)
            return new { shots = Array.Empty<object>(), assets = Array.Empty<object>(), newAssets = Array.Empty<object>(), shotCount = 0, frameCount = 0, newAssetCount = 0 };

        var existingNames = existingAssets.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dedupedNewAssets = new List<object>();
        if (parsed.assets != null)
        {
            foreach (var na in parsed.assets)
            {
                if (string.IsNullOrEmpty(na.name)) continue;
                if (existingNames.Contains(na.name)) continue;
                dedupedNewAssets.Add(new { na.assetType, na.name, na.description });
            }
        }

        var shotCount = parsed.shots?.Count ?? 0;
        var frameCount = parsed.shots?.Sum(s => s.frames?.Count ?? 0) ?? 0;

        var shotsList = parsed.shots?.Select(s => (object)new
        {
            s.shotName, s.shotNumber, s.shotSize, s.cameraMovement, s.duration,
            assetRefs = s.assetRefs ?? new List<string>(),
            frames = s.frames?.Select(f => new { f.frameType, f.description, f.order, f.startTime, f.duration }).ToList()
        }).ToList() ?? new List<object>();

        var assetsList = parsed.assets?.Select(a => (object)new { a.assetType, a.name, a.description }).ToList() ?? new List<object>();

        return new
        {
            shots = shotsList,
            assets = assetsList,
            newAssets = dedupedNewAssets,
            shotCount = shotCount,
            frameCount = frameCount,
            newAssetCount = dedupedNewAssets.Count,
            existingAssetCount = existingAssets.Count
        };
    }

    [HttpPost]
    [Route("/api/v1/ai/confirm-save-extraction")]
    public async Task<IActionResult> ConfirmSaveExtraction([FromBody] SaveExtractionRequest req)
    {
        try
        {
            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var existingAssets = await db.Assets
                .Where(a => a.ProjectId == req.ProjectId)
                .ToListAsync();

            var chapters = await (from s in db.Stories
                                   where s.ProjectId == req.ProjectId
                                   join c in db.StoryChapters on s.Id equals c.StoryId
                                   orderby c.SortOrder
                                   select c).ToListAsync();

            if (req.ChapterIdx < 0 || req.ChapterIdx >= chapters.Count)
                return Json(new { success = false, message = "章节索引无效" });

            var chapter = chapters[req.ChapterIdx];

            var cleanJson = ExtractJsonFromResponse(req.AiResponse);
            var parsed = JsonSerializer.Deserialize<ShotAssetExtractionResult>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed == null)
                return Json(new { success = false, message = "AI 返回格式解析失败" });

            var episode = await db.Episodes.FirstOrDefaultAsync(e => e.StoryChapterId == chapter.Id);
            if (episode == null)
            {
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

            var existingAssetNames = existingAssets.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newAssetEntries = new List<Asset>();
            var allAssetNames = new HashSet<string>(existingAssetNames, StringComparer.OrdinalIgnoreCase);

            if (parsed.assets != null)
            {
                foreach (var na in parsed.assets)
                {
                    if (string.IsNullOrEmpty(na.name)) continue;
                    if (existingAssetNames.Contains(na.name)) continue;

                    var asset = new Asset
                    {
                        ProjectId = req.ProjectId,
                        AssetType = ParseAssetType(na.assetType),
                        Name = na.name,
                        Description = na.description ?? "",
                        Order = 0
                    };
                    db.Assets.Add(asset);
                    await db.SaveChangesAsync();
                    newAssetEntries.Add(asset);
                    allAssetNames.Add(na.name);
                }
            }

            var shotCount = 0;
            var totalFrameCount = 0;
            if (parsed.shots != null)
            {
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

                    var assetRefs = new List<string>();
                    if (shotData.assetRefs != null)
                    {
                        foreach (var refName in shotData.assetRefs)
                        {
                            if (allAssetNames.Contains(refName))
                                assetRefs.Add(refName);
                            else
                                assetRefs.Add(refName);
                        }
                    }
                    shot.AssetRefs = string.Join(",", assetRefs);

                    db.Shots.Add(shot);
                    await db.SaveChangesAsync();

                    if (shotData.frames != null)
                    {
                        foreach (var frameData in shotData.frames.OrderBy(f => f.order))
                        {
                            db.ShotFrames.Add(new ShotFrame
                            {
                                ShotId = shot.Id,
                                ProjectId = req.ProjectId,
                                FrameType = frameData.frameType ?? "Middle",
                                Description = frameData.description ?? "",
                                Order = frameData.order,
                                StartTime = frameData.startTime,
                                Duration = frameData.duration
                            });
                            totalFrameCount++;
                        }
                    }

                    shotCount++;
                }
            }

            await db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                data = new
                {
                    shotCount = shotCount,
                    frameCount = totalFrameCount,
                    assetCount = newAssetEntries.Count,
                    assetNames = newAssetEntries.Select(a => a.Name).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion
}

public class ShotExtractionRequest
{
    public long ProjectId { get; set; }
    public int ChapterIdx { get; set; }
    public long ProviderId { get; set; }
    public List<long> SelectedAssetIds { get; set; }
    public string CustomPrompt { get; set; }
}

public class ShotAssetExtractionRequest
{
    public long ProjectId { get; set; }
    public int ChapterIdx { get; set; }
    public long ProviderId { get; set; }
    public List<long>? SelectedAssetIds { get; set; }
    public string? CustomPrompt { get; set; }
}

public class SaveExtractionRequest
{
    public long ProjectId { get; set; }
    public int ChapterIdx { get; set; }
    public string AiResponse { get; set; }
}

internal class StoryboardExtractionResult
{
    public List<ShotData>? shots { get; set; }
    public List<NewAssetData>? newAssets { get; set; }
}

internal class ShotAssetExtractionResult
{
    public List<ShotData>? shots { get; set; }
    public List<NewAssetData>? assets { get; set; }
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