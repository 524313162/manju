using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.AI;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.Service.Dtos;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using System.Text.Json;

namespace ManjuCraft.Web.Controllers;

public class AssetsController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IProjectService _projectService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWebHostEnvironment _env;
    private readonly IProjectDbContext _db;
    private readonly IAiAgentService _aiAgent;

    public AssetsController(IAssetService assetService, IProjectService projectService, IFileStorageService fileStorageService, IWebHostEnvironment env, IProjectDbContext db, IAiAgentService aiAgent)
    {
        _assetService = assetService;
        _projectService = projectService;
        _fileStorageService = fileStorageService;
        _env = env;
        _db = db;
        _aiAgent = aiAgent;
    }

    public async Task<IActionResult> Index(long projectId, AssetTypeEnum type = AssetTypeEnum.Actor)
    {
        if (projectId <= 0)
            return RedirectToAction("Index", "Projects");

        await LoadProjectsAsync();

        var typeName = type.DisplayName();
        var assets = await _assetService.GetByProjectAsync(projectId, type);

        ViewBag.ProjectId = projectId;
        ViewBag.Type = type;
        ViewBag.TypeName = typeName;
        ViewBag.Assets = assets;

        try
        {
            var proj = await _projectService.GetByIdAsync(projectId);
            ViewBag.ProjectName = proj?.Name ?? "";
        }
        catch { ViewBag.ProjectName = ""; }

        return View("Index");
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var projects = await _projectService.GetAllAsync();
            ViewBag.Projects = projects;
        }
        catch { ViewBag.Projects = new List<Project>(); }
    }

    [HttpGet]
    [Route("/Assets/ListByProject")]
    public async Task<IActionResult> ListByProject(long projectId, AssetTypeEnum? type = null)
    {
        var assets = await _assetService.GetByProjectAsync(projectId, type);
        var result = assets
            .OrderBy(a => a.Order)
            .ThenBy(a => a.Name)
            .Select(a => new { Id = a.Id.ToString(), a.Name, a.Description, a.AssetType, a.Order, ParentId = a.ParentId?.ToString() })
            .ToList();
        return Json(new { success = true, data = result });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name))
            return Content(ToJson(false, "名称不能为空"));

        await _assetService.CreateAsync(dto);
        return Content(ToJson(true));
    }

    [HttpPost]
    [Route("/Assets/BulkCreate")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateDto dto)
    {
        if (dto == null || dto.Assets == null || dto.Assets.Count == 0)
            return Json(new { success = false, message = "资产数据为空" });

        var result = await _assetService.BulkCreateAsync(dto);

        return Json(new { success = true, message = $"成功保存 {result.Count} 个资产", count = result.Count });
    }

    [HttpPost]
    [Route("/Assets/ClearAll")]
    public async Task<IActionResult> ClearAll([FromBody] JsonElement body)
    {
        try
        {
            var projectId = body.TryGetProperty("projectId", out var pid) ? pid.GetInt64() : 0L;
            if (projectId <= 0)
                return Json(new { success = false, message = "参数错误" });

            var assets = await _assetService.GetByProjectAsync(projectId);
            if (assets.Count == 0)
                return Json(new { success = false, message = "没有可清除的资产" });

            foreach (var a in assets)
                await _assetService.DeleteAsync(a.Id);

            return Json(new { success = true, message = $"已清除 {assets.Count} 个资产" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromBody] UpdateAssetDto dto)
    {
        if (string.IsNullOrEmpty(dto.Id))
            return Content(ToJson(false, "参数错误"));

        await _assetService.UpdateAsync(dto);
        return Content(ToJson(true));
    }

    [HttpPost]
    public async Task<IActionResult> Delete()
    {
        string body;
        using (var reader = new StreamReader(Request.Body)) { body = await reader.ReadToEndAsync(); }
        body = body.Trim();
        if (string.IsNullOrEmpty(body))
            return Content(ToJson(false, "参数错误"));

        using var doc = JsonDocument.Parse(body);
        var idStr = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : "";
        if (!Guid.TryParse(idStr, out var id))
            return Content(ToJson(false, "参数错误"));

        await DeleteResourceForAssetAsync(id);
        await _assetService.DeleteAsync(id);
        return Content(ToJson(true));
    }

    [HttpPost]
    public async Task<IActionResult> ReplaceResource()
    {
        Guid? assetId = null;
        string? fileUrl = null;
        long? projectId = null;

        var hasFileUpload = Request.HasFormContentType && Request.Form.Files.Count > 0;

        if (!hasFileUpload)
        {
            string body;
            using (var reader = new StreamReader(Request.Body)) { body = await reader.ReadToEndAsync(); }
            body = body.Trim();
            if (string.IsNullOrEmpty(body))
                return Content(ToJson(false, "参数错误"));

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var idStr = root.TryGetProperty("assetId", out var idProp) ? idProp.GetString() : "";
            if (Guid.TryParse(idStr, out var aid)) assetId = aid;
            fileUrl = root.TryGetProperty("fileUrl", out var urlProp) ? urlProp.GetString() : null;
        }

        if (assetId == null || assetId == Guid.Empty)
            return Content(ToJson(false, "参数错误"));

        var asset = await _assetService.GetByIdAsync(assetId.Value);
        if (asset == null)
            return Content(ToJson(false, "资产不存在"));

        if (hasFileUpload)
        {
            var file = Request.Form.Files.GetFile("uploadFile");
            if (file == null || file.Length == 0)
                return Content(ToJson(false, "请选择文件"));

            await SaveResourceForAssetAsync(asset, file);
            return Content(ToJson(true));
        }
        else if (!string.IsNullOrEmpty(fileUrl))
        {
            await SaveResourceFromUrlAsync(asset, fileUrl);
            return Content(ToJson(true));
        }

        return Content(ToJson(false, "请提供文件"));
    }

    [HttpPost]
    public async Task<IActionResult> ReplaceAudio()
    {
        await ReplaceResourceAsync();
        return Content(ToJson(true));
    }

    private async Task ReplaceResourceAsync()
    {
        string body;
        using (var reader = new StreamReader(Request.Body)) { body = await reader.ReadToEndAsync(); }
        body = body.Trim();
        if (string.IsNullOrEmpty(body)) return;

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var idStr = root.TryGetProperty("assetId", out var idProp) ? idProp.GetString() : "";
        if (!Guid.TryParse(idStr, out var assetId)) return;
        var fileUrl = root.TryGetProperty("fileUrl", out var urlProp) ? urlProp.GetString() : null;
        if (string.IsNullOrEmpty(fileUrl)) return;

        var asset = await _assetService.GetByIdAsync(assetId);
        if (asset == null) return;

        await SaveResourceFromUrlAsync(asset, fileUrl);
    }

    private async Task SaveResourceFromUrlAsync(Asset asset, string fileUrl)
    {
        await DeleteResourceForAssetAsync(asset.Id);

        var ext = Path.GetExtension(new Uri(fileUrl).AbsolutePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
        {
            switch (asset.AssetType)
            {
                case AssetTypeEnum.Actor:
                case AssetTypeEnum.Scene:
                case AssetTypeEnum.Prop:
                    ext = ".png"; break;
                case AssetTypeEnum.Bgm:
                    ext = ".mp3"; break;
            }
        }

        using var http = new HttpClient();
        var bytes = await http.GetByteArrayAsync(fileUrl);

        var assetTypeStr = asset.AssetType.ToString().ToLower();
        var path = await _fileStorageService.SaveAssetAsync(asset.ProjectId, assetTypeStr, asset.Id, bytes, ext);

        var ctx = HttpContext.RequestServices.GetRequiredService<ManjuCraft.Infrastructure.ProjectDbContext>();
        var resource = new Resource
        {
            MediaType = asset.AssetType == AssetTypeEnum.Bgm ? "audio" : "image",
            FilePath = path
        };
        ctx.Resources.Add(resource);
        await ctx.SaveChangesAsync();

        asset.ResourceId = resource.Id;
        await ctx.SaveChangesAsync();
    }

    private async Task SaveResourceForAssetAsync(Asset asset, IFormFile file)
    {
        await DeleteResourceForAssetAsync(asset.Id);

        var assetTypeStr = asset.AssetType.ToString().ToLower();
        var ext = DetermineFileExtension(file);
        var path = await _fileStorageService.SaveAssetAsync(asset.ProjectId, assetTypeStr, asset.Id, await ReadFileBytesAsync(file), ext);

        var ctx = HttpContext.RequestServices.GetRequiredService<ManjuCraft.Infrastructure.ProjectDbContext>();
        var resource = new Resource
        {
            MediaType = file.ContentType?.Split('/')[0] ?? "",
            FilePath = path
        };
        ctx.Resources.Add(resource);
        await ctx.SaveChangesAsync();

        asset.ResourceId = resource.Id;
        await ctx.SaveChangesAsync();
    }

    private async Task DeleteResourceForAssetAsync(Guid assetId)
    {
        var db = _env.WebRootPath;
        try
        {
            var ctx = HttpContext.RequestServices.GetRequiredService<ManjuCraft.Infrastructure.ProjectDbContext>();
            var asset = await ctx.Assets.FindAsync(assetId);
            if (asset?.ResourceId.HasValue == true)
            {
                var oldRes = await ctx.Resources.FindAsync(asset.ResourceId.Value);
                if (oldRes != null)
                {
                    var oldPath = Path.Combine(db, oldRes.FilePath.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                    ctx.Resources.Remove(oldRes);
                    await ctx.SaveChangesAsync();
                }
            }
            if (asset != null)
            {
                asset.ResourceId = null;
                await ctx.SaveChangesAsync();
            }
        }
        catch { }
    }

    private string DetermineFileExtension(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!string.IsNullOrEmpty(ext)) return ext;
        return (file.ContentType ?? "").ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/svg+xml" => ".svg",
            "audio/mpeg" => ".mp3",
            "audio/wav" => ".wav",
            "audio/ogg" => ".ogg",
            "audio/mp4" or "audio/aac" => ".m4a",
            _ => ext
        };
    }

    private async Task<byte[]> ReadFileBytesAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }

    private string ToJson(bool success, string? message = null)
    {
        var obj = new Dictionary<string, object> { { "success", success } };
        if (!string.IsNullOrEmpty(message))
            obj["message"] = message;
        return JsonSerializer.Serialize(obj);
    }

    [HttpGet]
    [Route("/Assets/GetGenerationTemplates")]
    public async Task<IActionResult> GetGenerationTemplates()
    {
        var templates = await _db.PromptTemplates
            .Where(t => t.TemplateType.StartsWith("AssetGeneration"))
            .ToListAsync();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in templates)
        {
            result[t.Name] = t.Content;
        }

        return Json(new { success = true, data = result });
    }

    [HttpPost]
    [Route("/Assets/GenerateCharacterImage")]
    public async Task<IActionResult> GenerateCharacterImage([FromBody] GenerateCharacterImageRequest request)
    {
        if (string.IsNullOrEmpty(request.CharacterPrompt))
            return Json(new { success = false, message = "提示词不能为空" });

        var (success, promptId, workflowType, message) = await _aiAgent.SubmitCharacterProfileAsync(
            request.SystemPrompt ?? "",
            request.CharacterPrompt,
            request.NegativePrompt,
            request.Width > 0 ? request.Width : 1792,
            request.Height > 0 ? request.Height : 1024);

        if (!success)
            return Json(new { success = false, message = message ?? "生成失败" });

        return Json(new { success = true, promptId, workflowType });
    }

    // ---- Asset binding endpoints (moved from AIGenerationController) ----

    [HttpPost]
    [Route("/api/v1/assets/extract-asset-info-save")]
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

            foreach (var asset in assetsList)
                if (asset.Id == Guid.Empty)
                    asset.Id = Guid.NewGuid();

            var nameToAsset = assetsList.ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);
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

    [HttpGet]
    [Route("/api/v1/assets/shot-frame-assets/{shotId}")]
    public async Task<IActionResult> GetShotFrameAssets(long shotId)
    {
        try
        {
            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();
            var frameIds = await db.ShotFrames.Where(f => f.ShotId == shotId).Select(f => f.Id).ToListAsync();
            if (frameIds.Count == 0)
                return Json(new { success = true, data = new List<object>() });

            var assets = await (from sfa in db.ShotFrameAssets
                                join a in db.Assets on sfa.AssetId equals a.Id
                                where frameIds.Contains(sfa.ShotFrameId)
                                select new { a.Name, a.AssetType }).Distinct().ToListAsync();
            return Json(new { success = true, data = assets });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/assets/save-shot-frame-assets")]
    public async Task<IActionResult> SaveShotFrameAssets([FromBody] JsonElement body)
    {
        try
        {
            var projectId = body.TryGetProperty("projectId", out var pid) ? pid.GetInt64() : 0L;
            var shotId = body.TryGetProperty("shotId", out var sid) ? sid.GetInt64() : 0L;
            if (projectId <= 0 || shotId <= 0)
                return Json(new { success = false, message = "参数错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();

            var shot = await db.Shots.FindAsync(shotId);
            if (shot == null)
                return Json(new { success = false, message = "分镜不存在" });

            var allAssetIds = new List<Guid>();

            var selectedNames = body.TryGetProperty("selectedAssetNames", out var san) && san.ValueKind == JsonValueKind.Array
                ? san.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();

            if (selectedNames.Count > 0)
            {
                var existingAssets = await db.Assets
                    .Where(a => a.ProjectId == projectId && selectedNames.Contains(a.Name))
                    .ToListAsync();
                foreach (var asset in existingAssets)
                    allAssetIds.Add(asset.Id);
            }

            var assetTypeMap = new Dictionary<string, AssetTypeEnum>
            {
                ["Actor"] = AssetTypeEnum.Actor, ["角色"] = AssetTypeEnum.Actor,
                ["Scene"] = AssetTypeEnum.Scene, ["场景"] = AssetTypeEnum.Scene,
                ["Prop"] = AssetTypeEnum.Prop, ["道具"] = AssetTypeEnum.Prop,
            };

            var existingNames = await db.Assets
                .Where(a => a.ProjectId == projectId)
                .Select(a => a.Name)
                .ToListAsync();
            var existingNameSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

            var newAssetRecords = new List<Asset>();
            var newAssetFrameBindings = new List<(int FrameIndex, Guid AssetId)>();

            var newAssetsProp = body.TryGetProperty("newAssets", out var nap) && nap.ValueKind == JsonValueKind.Array
                ? nap : default;

            if (newAssetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in newAssetsProp.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : "";
                    var assetTypeStr = item.TryGetProperty("assetType", out var at) ? at.GetString()?.Trim() : "";
                    var description = item.TryGetProperty("description", out var d) ? d.GetString()?.Trim() ?? "" : "";
                    var belongFrame = item.TryGetProperty("belongFrame", out var bf) ? bf.GetInt32() : -1;

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(assetTypeStr)) continue;
                    if (!assetTypeMap.TryGetValue(assetTypeStr, out var assetType)) continue;

                    Guid assetId;
                    if (existingNameSet.Contains(name))
                    {
                        var existing = await db.Assets.FirstAsync(a => a.ProjectId == projectId && a.Name == name);
                        assetId = existing.Id;
                    }
                    else
                    {
                        assetId = Guid.NewGuid();
                        newAssetRecords.Add(new Asset { Id = assetId, ProjectId = projectId, AssetType = assetType, Name = name, Description = description, Order = 0 });
                        existingNameSet.Add(name);
                    }

                    allAssetIds.Add(assetId);
                    newAssetFrameBindings.Add((belongFrame, assetId));
                }

                if (newAssetRecords.Count > 0)
                    await db.Assets.AddRangeAsync(newAssetRecords);
            }

            var frames = await db.ShotFrames.Where(f => f.ShotId == shotId).OrderBy(f => f.Order).ToListAsync();
            foreach (var (frameIdx, assetId) in newAssetFrameBindings)
            {
                var targetFrame = frameIdx >= 0 && frameIdx < frames.Count ? frames[frameIdx] : frames.FirstOrDefault();
                if (targetFrame == null) continue;
                if (!await db.ShotFrameAssets.AnyAsync(sfa => sfa.ShotFrameId == targetFrame.Id && sfa.AssetId == assetId))
                    db.ShotFrameAssets.Add(new ShotFrameAsset { ShotFrameId = targetFrame.Id, AssetId = assetId, Role = "", Order = 0 });
            }

            foreach (var assetId in allAssetIds)
            {
                if (newAssetFrameBindings.Any(b => b.AssetId == assetId)) continue;
                foreach (var frame in frames)
                {
                    if (!await db.ShotFrameAssets.AnyAsync(sfa => sfa.ShotFrameId == frame.Id && sfa.AssetId == assetId))
                        db.ShotFrameAssets.Add(new ShotFrameAsset { ShotFrameId = frame.Id, AssetId = assetId, Role = "", Order = 0 });
                }
            }

            await db.SaveChangesAsync();
            return Json(new { success = true, message = $"成功绑定 {allAssetIds.Count} 个资产（新建 {newAssetRecords.Count} 个）到分镜帧" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/assets/replace-frame-assets")]
    public async Task<IActionResult> ReplaceFrameAssets([FromBody] JsonElement body)
    {
        try
        {
            var projectId = body.TryGetProperty("projectId", out var pid) ? pid.GetInt64() : 0L;
            var shotId = body.TryGetProperty("shotId", out var sid) ? sid.GetInt64() : 0L;
            if (projectId <= 0 || shotId <= 0)
                return Json(new { success = false, message = "参数错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();
            var frames = await db.ShotFrames.Where(f => f.ShotId == shotId).OrderBy(f => f.Order).ToListAsync();
            if (frames.Count == 0)
                return Json(new { success = false, message = "该分镜没有帧" });

            var frameIds = frames.Select(f => f.Id).ToList();
            var existing = await db.ShotFrameAssets.Where(sfa => frameIds.Contains(sfa.ShotFrameId)).ToListAsync();
            db.ShotFrameAssets.RemoveRange(existing);

            var totalBound = 0;
            var frameAssetsProp = body.TryGetProperty("frameAssets", out var fa) && fa.ValueKind == JsonValueKind.Array ? fa : default;

            if (frameAssetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in frameAssetsProp.EnumerateArray())
                {
                    var frameIdx = entry.TryGetProperty("frameIdx", out var fi) ? fi.GetInt32() : -1;
                    var assetNames = entry.TryGetProperty("assetNames", out var an) && an.ValueKind == JsonValueKind.Array
                        ? an.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                        : new List<string>();
                    if (frameIdx < 0 || frameIdx >= frames.Count || assetNames.Count == 0) continue;

                    var targetFrame = frames[frameIdx];
                    var assets = await db.Assets.Where(a => a.ProjectId == projectId && assetNames.Contains(a.Name)).ToListAsync();
                    var order = 0;
                    foreach (var asset in assets)
                    {
                        db.ShotFrameAssets.Add(new ShotFrameAsset { ShotFrameId = targetFrame.Id, AssetId = asset.Id, Order = order++ });
                        totalBound++;
                    }
                }
            }

            await db.SaveChangesAsync();
            return Json(new { success = true, message = $"已更新 {totalBound} 个资产绑定到 {frames.Count} 帧" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("/api/v1/assets/remove-frame-asset")]
    public async Task<IActionResult> RemoveFrameAsset([FromBody] JsonElement body)
    {
        try
        {
            var shotId = body.TryGetProperty("shotId", out var sid) ? sid.GetInt64() : 0L;
            var assetIdStr = body.TryGetProperty("assetId", out var aid) ? aid.GetString() : "";
            if (shotId <= 0 || string.IsNullOrEmpty(assetIdStr))
                return Json(new { success = false, message = "参数错误" });

            if (!Guid.TryParse(assetIdStr, out var assetId))
                return Json(new { success = false, message = "资产ID格式错误" });

            var db = HttpContext.RequestServices.GetRequiredService<ProjectDbContext>();
            var frameIds = await db.ShotFrames.Where(f => f.ShotId == shotId).Select(f => f.Id).ToListAsync();
            var targets = await db.ShotFrameAssets
                .Where(sfa => frameIds.Contains(sfa.ShotFrameId) && sfa.AssetId == assetId)
                .ToListAsync();

            if (targets.Count == 0)
                return Json(new { success = false, message = "未找到绑定的资产" });

            db.ShotFrameAssets.RemoveRange(targets);
            await db.SaveChangesAsync();
            return Json(new { success = true, message = $"已从 {targets.Count} 个帧中移除" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}

public class GenerateCharacterImageRequest
{
    public string? SystemPrompt { get; set; }
    public string CharacterPrompt { get; set; } = "";
    public string? NegativePrompt { get; set; }
    public int Width { get; set; } = 1792;
    public int Height { get; set; } = 1024;
}
