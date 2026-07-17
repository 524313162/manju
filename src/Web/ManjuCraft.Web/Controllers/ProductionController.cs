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
            var selectedGuids = req.SelectedAssetIds.Select(s => { Guid.TryParse(s, out var g); return g; }).Where(g => g != Guid.Empty).ToList();
            var existingAssets = await db.Assets
                .Where(a => a.ProjectId == req.ProjectId && selectedGuids.Contains(a.Id))
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
            var newAssetEntries = new Dictionary<string, Guid>(); // name -> id
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
                    ShotNumber = shotData.shotNumber ?? $"SH{shotCount + 1:D3}",
                    Duration = shotData.duration,
                    Order = shotCount
                };

                db.Shots.Add(shot);
                await db.SaveChangesAsync();

                // Save frames
                if (shotData.frames != null)
                {
                    var frameOrder = 0;
                    foreach (var frameData in shotData.frames.OrderBy(f => f.order))
                    {
                        db.ShotFrames.Add(new ShotFrame
                        {
                            ShotId = shot.Id,
                            ProjectId = req.ProjectId,
                            ShotNumber = shot.ShotNumber,
                            FrameType = frameData.frameType ?? (frameOrder == 0 ? "First" : "Last"),
                            NarrativeDescription = frameData.narrativeDescription ?? frameData.description ?? "",
                            GeneratePrompt = frameData.generatePrompt ?? "",
                            CameraMovement = frameData.cameraMovement ?? "",
                            ShotSize = frameData.shotSize ?? shotData.shotSize ?? "",
                            StartTime = frameData.startTime,
                            Duration = frameData.duration,
                            Order = frameOrder++
                        });
                    }
                    await db.SaveChangesAsync();

                    // Bind per-frame assets
                    var savedFrames = await db.ShotFrames
                        .Where(f => f.ShotId == shot.Id)
                        .OrderBy(f => f.Order)
                        .ToListAsync();

                    var frameIndex = 0;
                    foreach (var frameDataIn in shotData.frames.OrderBy(f => f.order))
                    {
                        if (frameIndex >= savedFrames.Count) break;
                        var savedFrame = savedFrames[frameIndex];
                        if (frameDataIn.assetRefs != null)
                        {
                            var assetOrder = 0;
                            foreach (var refName in frameDataIn.assetRefs)
                            {
                                if (allAssetNames.Contains(refName))
                                {
                                    var asset = existingAssets.FirstOrDefault(a => a.Name == refName)
                                        ?? (await db.Assets.FirstOrDefaultAsync(a => a.ProjectId == req.ProjectId && a.Name == refName));
                                    if (asset != null)
                                    {
                                        var exists = await db.ShotFrameAssets
                                            .AnyAsync(sfa => sfa.ShotFrameId == savedFrame.Id && sfa.AssetId == asset.Id);
                                        if (!exists)
                                        {
                                            db.ShotFrameAssets.Add(new ShotFrameAsset
                                            {
                                                ShotFrameId = savedFrame.Id,
                                                AssetId = asset.Id,
                                                Role = "",
                                                Order = assetOrder++
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        frameIndex++;
                    }
                    await db.SaveChangesAsync();
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
            // Debug logging
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<ProductionController>>();
            logger.LogInformation("ExtractShotsAndAssets called: ProjectId={ProjectId}, ChapterIdx={ChapterIdx}, ProviderId={ProviderId}", 
                req?.ProjectId, req?.ChapterIdx, req?.ProviderId);

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

            // 优先使用前端传入的 CustomPrompt 作为 system prompt；为空则回退数据库模板
            var sysPrompt = !string.IsNullOrWhiteSpace(req.CustomPrompt)
                ? req.CustomPrompt!.Trim()
                : (await db.PromptTemplates
                    .Where(p => p.TemplateType == "ShotAssetExtraction" && p.IsDefault)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync())?.Content ?? "你是一个专业的漫剧分镜师和资产管理员。";

            var assetText = existingAssets.OrderBy(a => a.Order).ThenBy(a => a.Name)
                .Aggregate("", (acc, a) =>
                {
                    var desc = string.IsNullOrEmpty(a.Description) ? "" : $"（{a.Description}）";
                    return acc + $"- {a.Name} [{a.AssetType}]{desc}\n";
                });

            // user prompt 只包含章节内容和资产列表，不再包含 CustomPrompt
            var userPrompt = $"章节内容：\n\n{chapter.Content ?? ""}\n\n现有资产列表：\n{(string.IsNullOrEmpty(assetText) ? "（暂无资产）" : assetText)}";

            var aiService = HttpContext.RequestServices.GetRequiredService<IAiAgentService>();
            var result = await aiService.ChatAsync(
                req.ProviderId,
                sysPrompt,
                userPrompt,
                HttpContext.RequestAborted);

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
            s.shotNumber, s.duration,
            assetRefs = s.assetRefs ?? new List<string>(),
            frames = s.frames?.Select(f => new { f.frameType, narrativeDescription = f.narrativeDescription ?? f.description, generatePrompt = f.generatePrompt, f.cameraMovement, f.shotSize, f.dialogue, f.order, f.startTime, f.duration }).ToList()
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
                        ShotNumber = shotData.shotNumber ?? $"SH{shotCount + 1:D3}",
                        Duration = shotData.duration,
                        Order = shotCount
                    };

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
                                ShotNumber = shot.ShotNumber,
                                FrameType = frameData.frameType ?? "Middle",
                                NarrativeDescription = frameData.narrativeDescription ?? frameData.description ?? "",
                                GeneratePrompt = frameData.generatePrompt ?? "",
                                Dialogue = frameData.dialogue ?? "",
                                CameraMovement = frameData.cameraMovement ?? "",
                                ShotSize = frameData.shotSize ?? shotData.shotSize ?? "",
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

            // Bind per-frame assets
            var allSavedShots = await db.Shots.Where(s => s.EpisodeId == episode.Id).OrderBy(s => s.Order).ToListAsync();
            var shotIdx = 0;
            foreach (var shotData in parsed.shots ?? new List<ShotData>())
            {
                if (shotData.frames == null || shotIdx >= allSavedShots.Count) { shotIdx++; continue; }
                var savedFrames = await db.ShotFrames.Where(f => f.ShotId == allSavedShots[shotIdx].Id).OrderBy(f => f.Order).ToListAsync();
                var frameIdx = 0;
                foreach (var frameData in shotData.frames.OrderBy(f => f.order))
                {
                    if (frameIdx >= savedFrames.Count) break;
                    if (frameData.assetRefs != null)
                    {
                        var assetOrder = 0;
                        foreach (var refName in frameData.assetRefs)
                        {
                            if (!allAssetNames.Contains(refName)) continue;
                            var asset = existingAssets.FirstOrDefault(a => a.Name == refName)
                                ?? newAssetEntries.FirstOrDefault(a => a.Name == refName);
                            if (asset != null)
                            {
                                var exists = await db.ShotFrameAssets
                                    .AnyAsync(sfa => sfa.ShotFrameId == savedFrames[frameIdx].Id && sfa.AssetId == asset.Id);
                                if (!exists)
                                {
                                    db.ShotFrameAssets.Add(new ShotFrameAsset
                                    {
                                        ShotFrameId = savedFrames[frameIdx].Id,
                                        AssetId = asset.Id,
                                        Role = "",
                                        Order = assetOrder++
                                    });
                                }
                            }
                        }
                    }
                    frameIdx++;
                }
                shotIdx++;
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

    [HttpGet]
    [Route("/api/v1/production/template")]
    public async Task<IActionResult> GetTemplate([FromQuery] string type)
    {
        var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();
        var tpl = await db.PromptTemplates
            .Where(p => p.TemplateType == type)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();
        if (tpl == null)
            return Json(new { success = false, message = "模板不存在" });
        return Json(new { success = true, content = tpl.Content, name = tpl.Name });
    }

    [HttpGet]
    [Route("/api/v1/production/shots")]
    public async Task<IActionResult> GetShotsByChapter([FromQuery] long projectId, [FromQuery] int chapterIdx)
    {
        try
        {
            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var story = await db.Stories.FirstOrDefaultAsync(s => s.ProjectId == projectId);
            if (story == null)
                return Json(new { success = false, message = "未找到剧本" });

            var chapters = await db.StoryChapters
                .Where(c => c.StoryId == story.Id)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            if (chapterIdx < 0 || chapterIdx >= chapters.Count)
                return Json(new { success = false, message = "章节索引无效" });

            var chapter = chapters[chapterIdx];

            var episode = await db.Episodes.FirstOrDefaultAsync(e => e.StoryChapterId == chapter.Id);
            if (episode == null)
                return Json(new { success = true, data = new { shots = new List<object>(), episodeId = 0L } });

            var shots = await db.Shots
                .Where(s => s.EpisodeId == episode.Id)
                .OrderBy(s => s.Order)
                .ToListAsync();

            var shotDtos = new List<object>();
            var shotIds = shots.Select(s => s.Id).ToList();
            var shotResourceIds = shots.Where(s => s.ResourceId.HasValue).Select(s => s.ResourceId!.Value).ToList();
            var allResources = shotResourceIds.Count > 0
                ? await db.Resources.Where(r => shotResourceIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r)
                : new Dictionary<long, Resource>();

            foreach (var shot in shots)
            {
                var frames = await db.ShotFrames
                    .Where(f => f.ShotId == shot.Id)
                    .OrderBy(f => f.Order)
                    .Select(f => new
                    {
                        f.Id,
                        f.FrameType,
                        NarrativeDescription = f.NarrativeDescription,
                        GeneratePrompt = f.GeneratePrompt,
                        f.CameraMovement,
                        f.ShotSize,
                        f.Dialogue,
                        f.Order,
                        f.StartTime,
                        f.Duration,
                        f.ResourceId
                    })
                    .ToListAsync();

                // Get frame assets from ShotFrameAsset join table
                var frameIds = frames.Select(f => f.Id).ToList();
                var shotFrameAssets = await db.ShotFrameAssets
                    .Where(sfa => frameIds.Contains(sfa.ShotFrameId))
                    .Include(sfa => sfa.Asset)
                    .ThenInclude(a => a.Resource)
                    .ToListAsync();

                // Group assets by frame
                var assetsByFrameId = shotFrameAssets
                    .GroupBy(sfa => sfa.ShotFrameId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Build frames with their assets
                var frameIdsList = frames.Select(f => f.Id).ToList();
                var frameResourceIds = frames.Where(f => f.ResourceId.HasValue).Select(f => f.ResourceId!.Value).ToList();
                var frameResources = frameResourceIds.Count > 0
                    ? await db.Resources.Where(r => frameResourceIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r)
                    : new Dictionary<long, Resource>();

                var framesWithAssets = frames.Select(f =>
                {
                    var frameAssetList = new List<object>();
                    if (assetsByFrameId.TryGetValue(f.Id, out var sfas))
                    {
                        var seen = new HashSet<Guid>();
                        foreach (var sfa in sfas)
                        {
                            var a = sfa.Asset;
                            if (a == null || seen.Contains(a.Id)) continue;
                            seen.Add(a.Id);
                            var resource = a.Resource;
                            frameAssetList.Add(new
                            {
                                a.Id,
                                a.Name,
                                a.AssetType,
                                a.Description,
                                ResourceId = a.ResourceId,
                                ResourceFilePath = resource?.FilePath,
                                ResourceMediaType = resource?.MediaType
                            });
                        }
                    }
                    var frameResource = f.ResourceId.HasValue && frameResources.TryGetValue(f.ResourceId.Value, out var fr) ? fr : null;
                    return new
                    {
                        f.Id,
                        f.FrameType,
                        NarrativeDescription = f.NarrativeDescription,
                        GeneratePrompt = f.GeneratePrompt,
                        f.CameraMovement,
                        f.ShotSize,
                        f.Dialogue,
                        f.Order,
                        f.StartTime,
                        f.Duration,
                        f.ResourceId,
                        ImagePath = frameResource?.FilePath,
                        Assets = frameAssetList
                    };
                }).ToList();

                // All shot-level assets (deduplicated)
                var assetRefs = new List<object>();
                {
                    var seenAssetIds = new HashSet<Guid>();
                    foreach (var sfa in shotFrameAssets)
                    {
                        var asset = sfa.Asset;
                        if (asset == null || seenAssetIds.Contains(asset.Id)) continue;
                        seenAssetIds.Add(asset.Id);
                        var resource = asset.Resource;
                        assetRefs.Add(new
                        {
                            asset.Id,
                            asset.Name,
                            asset.AssetType,
                            asset.Description,
                            ResourceId = asset.ResourceId,
                            ResourceFilePath = resource?.FilePath,
                            ResourceMediaType = resource?.MediaType
                        });
                    }
                }

                var shotResource = shot.ResourceId.HasValue && allResources.TryGetValue(shot.ResourceId.Value, out var sr) ? sr : null;
                shotDtos.Add(new
                {
                    shot.Id,
                    shot.ShotNumber,
                    shot.Duration,
                    shot.Order,
                    Assets = assetRefs,
                    VideoUrl = shotResource?.FilePath ?? "",
                    ResourceId = shot.ResourceId,
                    ResourceFilePath = shotResource?.FilePath,
                    ResourceMediaType = shotResource?.MediaType,
                    frames = framesWithAssets
                });
            }

            return Json(new { success = true, data = new { episodeId = episode.Id, shots = shotDtos } });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/production/extract-asset-info-save")]
    public async Task<IActionResult> ExtractAssetInfoSave([FromBody] JsonElement body)
    {
        try
        {
            var projectId = body.TryGetProperty("projectId", out var pid) ? pid.GetInt64() : 0L;
            if (projectId <= 0)
                return Json(new { success = false, message = "缺少项目ID" });

            if (!body.TryGetProperty("assets", out var assetsProp))
                return Json(new { success = false, message = "缺少资产数据" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var assetTypeMap = new Dictionary<string, AssetTypeEnum>
            {
                ["Actor"] = AssetTypeEnum.Actor,
                ["角色"] = AssetTypeEnum.Actor,
                ["Scene"] = AssetTypeEnum.Scene,
                ["场景"] = AssetTypeEnum.Scene,
                ["Bgm"] = AssetTypeEnum.Bgm,
                ["BGM"] = AssetTypeEnum.Bgm,
                ["Prop"] = AssetTypeEnum.Prop,
                ["道具"] = AssetTypeEnum.Prop,
                ["VoiceVoice"] = AssetTypeEnum.VoiceVoice,
                ["声音"] = AssetTypeEnum.VoiceVoice,
            };

            var assetsList = new List<Asset>();
            var existingNames = await db.Assets
                .Where(a => a.ProjectId == projectId)
                .Select(a => a.Name)
                .ToListAsync();
            var existingNameSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

            foreach (var item in assetsProp.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : "";
                var assetTypeStr = item.TryGetProperty("assetType", out var at) ? at.GetString()?.Trim() : "";
                var description = item.TryGetProperty("description", out var d) ? d.GetString()?.Trim() ?? "" : "";
                var parentName = item.TryGetProperty("belong", out var bn) ? bn.GetString()?.Trim() : null;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(assetTypeStr))
                    continue;

                if (!assetTypeMap.TryGetValue(assetTypeStr, out var assetType))
                    continue;

                if (existingNameSet.Contains(name))
                    continue;

                var maxOrder = 0;
                var lastAsset = await db.Assets
                    .Where(a => a.ProjectId == projectId && a.AssetType == assetType)
                    .OrderByDescending(a => a.Order)
                    .FirstOrDefaultAsync();
                if (lastAsset != null)
                    maxOrder = lastAsset.Order;

                assetsList.Add(new Asset
                {
                    ProjectId = projectId,
                    AssetType = assetType,
                    Name = name,
                    Description = description,
                    Order = maxOrder + 1
                });

                existingNameSet.Add(name);
            }

            // Pre-assign IDs
            foreach (var asset in assetsList)
            {
                if (asset.Id == Guid.Empty)
                    asset.Id = Guid.NewGuid();
            }

            // Set ParentId from belong field (second pass)
            var nameToAsset = assetsList.ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);
            var allByName = existingNames.ToDictionary(n => n, n => (Asset?)null, StringComparer.OrdinalIgnoreCase);
            foreach (var item in assetsProp.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : "";
                var parentName = item.TryGetProperty("belong", out var bn) ? bn.GetString()?.Trim() : null;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(parentName)) continue;
                if (nameToAsset.TryGetValue(name, out var child) && nameToAsset.TryGetValue(parentName, out var parent))
                    child.ParentId = parent.Id;
            }

            if (assetsList.Count == 0)
                return Json(new { success = false, message = "没有需要新增的资产（可能已存在）" });

            await db.Assets.AddRangeAsync(assetsList);
            await db.SaveChangesAsync();

            return Json(new { success = true, message = $"成功保存 {assetsList.Count} 个资产", count = assetsList.Count });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/production/clear-shots")]
    public async Task<IActionResult> ClearShots([FromBody] JsonElement body)
    {
        try
        {
            var projectId = body.TryGetProperty("projectId", out var pid) ? pid.GetInt64() : 0L;
            var chapterIdx = body.TryGetProperty("chapterIdx", out var cidx) ? cidx.GetInt32() : -1;

            if (projectId <= 0 || chapterIdx < 0)
                return Json(new { success = false, message = "参数错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var story = await db.Stories.FirstOrDefaultAsync(s => s.ProjectId == projectId);
            if (story == null)
                return Json(new { success = false, message = "未找到剧本" });

            var chapters = await db.StoryChapters
                .Where(c => c.StoryId == story.Id)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            if (chapterIdx >= chapters.Count)
                return Json(new { success = false, message = "章节索引无效" });

            var chapter = chapters[chapterIdx];

            var episode = await db.Episodes.FirstOrDefaultAsync(e => e.StoryChapterId == chapter.Id);
            if (episode == null)
                return Json(new { success = true, message = "该章节暂无分镜数据" });

            var shots = await db.Shots.Where(s => s.EpisodeId == episode.Id).ToListAsync();
            if (shots.Count == 0)
                return Json(new { success = true, message = "该章节暂无分镜数据" });

            var shotIds = shots.Select(s => s.Id).ToList();
            var frames = await db.ShotFrames.Where(f => shotIds.Contains(f.ShotId)).ToListAsync();

            db.ShotFrames.RemoveRange(frames);
            db.Shots.RemoveRange(shots);
            await db.SaveChangesAsync();

            return Json(new { success = true, message = $"已清空 {shots.Count} 个分镜及 {frames.Count} 个帧" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/production/save-frame-image")]
    public async Task<IActionResult> SaveFrameImage([FromBody] JsonElement body)
    {
        try
        {
            var imageUrl = body.TryGetProperty("imageUrl", out var iu) ? iu.GetString() : "";
            var frameId = body.TryGetProperty("frameId", out var fid) ? fid.GetInt64() : 0L;

            if (frameId <= 0 || string.IsNullOrEmpty(imageUrl))
                return Json(new { success = false, message = "参数错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();
            var frame = await db.ShotFrames.FindAsync(frameId);
            if (frame == null)
                return Json(new { success = false, message = "帧不存在" });

            var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

            var fileName = $"frame_{frameId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
            var wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var targetDir = Path.Combine(wwwRoot, "asset", "frame-images");
            Directory.CreateDirectory(targetDir);
            var targetPath = Path.Combine(targetDir, fileName);
            await System.IO.File.WriteAllBytesAsync(targetPath, imageBytes);

            var relativePath = "/asset/frame-images/" + fileName;
            var resource = new ManjuCraft.Domain.Models.Resource
            {
                MediaType = "image/png",
                FilePath = relativePath
            };
            db.Resources.Add(resource);
            await db.SaveChangesAsync();

            frame.ResourceId = resource.Id;
            await db.SaveChangesAsync();

            return Json(new { success = true, message = "帧图片已保存", imagePath = relativePath });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/production/save-shot-video")]
    public async Task<IActionResult> SaveShotVideo([FromBody] JsonElement body)
    {
        try
        {
            var videoUrl = body.TryGetProperty("videoUrl", out var vu) ? vu.GetString() : "";
            var shotId = body.TryGetProperty("shotId", out var sid) ? sid.GetInt64() : 0L;

            if (shotId <= 0 || string.IsNullOrEmpty(videoUrl))
                return Json(new { success = false, message = "参数错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();
            var shot = await db.Shots.FindAsync(shotId);
            if (shot == null)
                return Json(new { success = false, message = "分镜不存在" });

            var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
            var videoBytes = await httpClient.GetByteArrayAsync(videoUrl);

            var fileName = $"shot_video_{shotId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.mp4";
            var wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var targetDir = Path.Combine(wwwRoot, "asset", "frame-images");
            Directory.CreateDirectory(targetDir);
            var targetPath = Path.Combine(targetDir, fileName);
            await System.IO.File.WriteAllBytesAsync(targetPath, videoBytes);

            var relativePath = "/asset/frame-images/" + fileName;
            var resource = new ManjuCraft.Domain.Models.Resource
            {
                MediaType = "video/mp4",
                FilePath = relativePath
            };
            db.Resources.Add(resource);
            await db.SaveChangesAsync();

            shot.ResourceId = resource.Id;
            await db.SaveChangesAsync();

            return Json(new { success = true, message = "分镜视频已保存", videoPath = relativePath });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

}

public class ShotExtractionRequest
{
    public long ProjectId { get; set; }
    public int ChapterIdx { get; set; }
    public long ProviderId { get; set; }
    public List<string> SelectedAssetIds { get; set; }
    public string CustomPrompt { get; set; }
}

public class ShotAssetExtractionRequest
{
    public long ProjectId { get; set; }
    public int ChapterIdx { get; set; }
    public long ProviderId { get; set; }
    public List<string>? SelectedAssetIds { get; set; }
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
    public string? description { get; set; }
    public List<string>? assetRefs { get; set; }
    public List<FrameData>? frames { get; set; }
}

internal class FrameData
{
    public string? frameType { get; set; }
    public string? description { get; set; }
    public string? narrativeDescription { get; set; }
    public string? generatePrompt { get; set; }
    public string? cameraMovement { get; set; }
    public string? shotSize { get; set; }
    public string? dialogue { get; set; }
    public List<string>? assetRefs { get; set; }
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