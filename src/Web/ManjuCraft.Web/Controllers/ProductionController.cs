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
                    ShotSize = shotData.shotSize ?? "",
                    CameraMovement = shotData.cameraMovement ?? "",
                    Duration = shotData.duration,
                    Order = shotCount,
                    Description = shotData.shotName ?? ""
                };

                db.Shots.Add(shot);
                await db.SaveChangesAsync();

                // Save shot assets
                if (shotData.assetRefs != null)
                {
                    var order = 0;
                    foreach (var refName in shotData.assetRefs)
                    {
                        if (allAssetNames.Contains(refName))
                        {
                            var asset = existingAssets.FirstOrDefault(a => a.Name == refName);
                            if (asset != null)
                            {
                                db.ShotAssets.Add(new ShotAsset
                                {
                                    ShotId = shot.Id,
                                    AssetId = asset.Id,
                                    Order = order++
                                });
                            }
                        }
                    }
                    await db.SaveChangesAsync();
                }

                // Save frames
                if (shotData.frames != null)
                {
                    foreach (var frameData in shotData.frames.OrderBy(f => f.order))
                    {
                        var frame = new ShotFrame
                        {
                            ShotId = shot.Id,
                            ProjectId = req.ProjectId,
                            ShotNumber = shot.ShotNumber,
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
                        ShotNumber = shotData.shotNumber ?? $"SH{shotCount + 1:D3}",
                        ShotSize = shotData.shotSize ?? "",
                        CameraMovement = shotData.cameraMovement ?? "",
                        Duration = shotData.duration,
                        Order = shotCount,
                        Description = shotData.shotName ?? ""
                    };

                    db.Shots.Add(shot);
                    await db.SaveChangesAsync();

                    // Save shot assets
                    if (shotData.assetRefs != null)
                    {
                        var order = 0;
                        foreach (var refName in shotData.assetRefs)
                        {
                            if (allAssetNames.Contains(refName))
                            {
                                var asset = existingAssets.FirstOrDefault(a => a.Name == refName);
                                if (asset != null)
                                {
                                    db.ShotAssets.Add(new ShotAsset
                                    {
                                        ShotId = shot.Id,
                                        AssetId = asset.Id,
                                        Order = order++
                                    });
                                }
                            }
                        }
                        await db.SaveChangesAsync();
                    }

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

            // Get shot assets
            var shotAssets = await db.ShotAssets
                .Where(sa => shots.Select(s => s.Id).Contains(sa.ShotId))
                .Include(sa => sa.Asset)
                .OrderBy(sa => sa.ShotId)
                .ThenBy(sa => sa.Order)
                .ToListAsync();

            var shotAssetsByShot = shotAssets.GroupBy(sa => sa.ShotId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var shotDtos = new List<object>();
            foreach (var shot in shots)
            {
                var frames = await db.ShotFrames
                    .Where(f => f.ShotId == shot.Id)
                    .OrderBy(f => f.Order)
                    .Select(f => new
                    {
                        f.Id,
                        f.FrameType,
                        f.Description,
                        f.Order,
                        f.StartTime,
                        f.Duration,
                        f.ResourceId
                    })
                    .ToListAsync();

                // Get shot assets from ShotAsset join table
                var shotAssetsList = shotAssetsByShot.TryGetValue(shot.Id, out var sas) ? sas.ToList() : new List<ShotAsset>();
                var shotAssetIds = shotAssetsList.Select(sa => sa.AssetId).ToList();
                var assetResources = await db.Assets
                    .Where(a => shotAssetIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.ResourceId, a.Resource })
                    .ToDictionaryAsync(a => a.Id);

                var assetRefs = new List<object>();
                if (shotAssetsList.Count > 0)
                {
                    foreach (var sa in shotAssetsList)
                    {
                        var asset = sa.Asset;
                        var resource = assetResources.TryGetValue(asset.Id, out var ar) ? ar.Resource : asset.Resource;
                        assetRefs.Add(new
                        {
                            asset.Id,
                            asset.Name,
                            asset.AssetType,
                            asset.Description,
                            ResourceId = asset.ResourceId,
                            ResourceFilePath = resource?.FilePath,
                            ResourceMediaType = resource?.MediaType,
                            sa.Role,
                            sa.Order
                        });
                    }
                }

                shotDtos.Add(new
                {
                    shot.Id,
                    shot.ShotNumber,
                    ShotName = shot.Description,
                    shot.ShotSize,
                    shot.CameraMovement,
                    shot.Duration,
                    shot.Order,
                    shot.Description,
                    Assets = assetRefs,
                    StoryboardUrl = "",
                    VideoUrl = "",
                    frames
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
    [Route("/api/v1/production/extract-asset-info")]
    public async Task<IActionResult> ExtractAssetInfo([FromBody] JsonElement body)
    {
        try
        {
            var providerId = body.TryGetProperty("providerId", out var pid) ? pid.GetInt64() : 0L;
            var template = body.TryGetProperty("template", out var tpl) ? tpl.GetString() : null;
            var chapterIds = body.TryGetProperty("chapterIds", out var cids) && cids.ValueKind == JsonValueKind.Array
                ? cids.EnumerateArray().Select(e => e.GetInt64()).ToList()
                : new List<long>();

            if (chapterIds.Count == 0)
                return Json(new { success = false, message = "请选择至少一个章节" });

            if (providerId <= 0)
                return Json(new { success = false, message = "请选择 AI 提供者" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var chapters = await db.StoryChapters
                .Where(c => chapterIds.Contains(c.Id))
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            if (chapters.Count == 0)
                return Json(new { success = false, message = "未找到选中的章节" });

            var storyId = chapters[0].StoryId;
            var story = await db.Stories.FindAsync(storyId);
            if (story == null)
                return Json(new { success = false, message = "剧本不存在" });
            var projectId = story.ProjectId;

            var existingAssets = await db.Assets
                .Where(a => a.ProjectId == projectId)
                .Select(a => new { a.Name, a.AssetType, a.Description })
                .ToListAsync();

            var existingAssetsText = string.Join("\n", existingAssets.Select(a => $"- {a.Name} ({a.AssetType})"));

            var chaptersText = string.Join("\n\n", chapters.Select(c =>
                $"【第{c.ChapterNumber}章 {c.ChapterName}】\n{c.Content}"));

            var templateContent = template;
            if (string.IsNullOrEmpty(templateContent))
            {
                var t = await db.PromptTemplates
                    .Where(p => p.TemplateType == "AssetExtraction")
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync();
                templateContent = t?.Content ?? "";
            }

            var userMsg = $"## 已有项目资产\n{existingAssetsText}\n\n## 章节内容\n{chaptersText}\n\n请严格按JSON格式输出提取的资产信息。";

            var aiService = HttpContext.RequestServices.GetRequiredService<IAiAgentService>();
            var result = await aiService.ChatAsync(providerId, templateContent, userMsg);
            if (!result.success)
                return Json(new { success = false, message = result.message ?? "提取失败" });

            if (result.isComfyui)
                return Json(new { success = true, data = result.data, promptId = result.promptId, workflowType = result.workflowType, isComfyui = true });

            return Json(new { success = true, data = result.data });
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
    [Route("/api/v1/production/shots/{shotId}/assets")]
    public async Task<IActionResult> SaveShotAssets(long shotId, [FromBody] SaveShotAssetsRequest req)
    {
        try
        {
            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var shot = await db.Shots.FindAsync(shotId);
            if (shot == null)
                return Json(new { success = false, message = "分镜不存在" });

            // Remove existing shot assets
            var existing = await db.ShotAssets.Where(sa => sa.ShotId == shotId).ToListAsync();
            db.ShotAssets.RemoveRange(existing);

            // Add new shot assets with roles
            if (req.Assets != null && req.Assets.Count > 0)
            {
                var assets = await db.Assets
                    .Where(a => req.Assets.Select(x => x.Name).Contains(a.Name))
                    .ToListAsync();

                var assetDict = assets.ToDictionary(a => a.Name);
                var order = 0;

                foreach (var reqAsset in req.Assets)
                {
                    if (assetDict.TryGetValue(reqAsset.Name, out var asset))
                    {
                        db.ShotAssets.Add(new ShotAsset
                        {
                            ShotId = shotId,
                            AssetId = asset.Id,
                            Role = reqAsset.Role ?? "",
                            Order = order++
                        });
                    }
                }
            }

            await db.SaveChangesAsync();

            return Json(new { success = true, message = "资产绑定保存成功" });
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

public class SaveShotAssetsRequest
{
    public List<ShotAssetBinding> Assets { get; set; }
}

public class ShotAssetBinding
{
    public string Name { get; set; }
    public string Role { get; set; }
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