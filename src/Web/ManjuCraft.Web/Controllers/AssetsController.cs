using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service;
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

    public AssetsController(IAssetService assetService, IProjectService projectService, IFileStorageService fileStorageService, IWebHostEnvironment env, IProjectDbContext db)
    {
        _assetService = assetService;
        _projectService = projectService;
        _fileStorageService = fileStorageService;
        _env = env;
        _db = db;
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
    public async Task<IActionResult> Create(long? projectId, AssetTypeEnum? assetType, string? name, string? description, string? parentId, int order = 0)
    {
        if (string.IsNullOrEmpty(name))
            return Content(ToJson(false, "名称不能为空"));

        var asset = new Asset
        {
            ProjectId = projectId ?? 0,
            AssetType = assetType ?? AssetTypeEnum.Actor,
            Name = name,
            Description = description ?? "",
            Order = order
        };

        if (!string.IsNullOrEmpty(parentId) && Guid.TryParse(parentId, out var pid))
            asset.ParentId = pid;

        await _assetService.CreateAsync(asset);
        return Content(ToJson(true));
    }

    [HttpPost]
    [Route("/Assets/BulkCreate")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateRequest req)
    {
        if (req == null || req.Assets == null || req.Assets.Count == 0)
            return Json(new { success = false, message = "资产数据为空" });

        var existingAssets = await _assetService.GetByProjectAsync(req.ProjectId);
        var existingByName = existingAssets.ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Asset>();
        var toUpdate = new List<Asset>();
        var nameToAsset = new Dictionary<string, Asset>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in req.Assets)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.AssetType))
                continue;

            var name = item.Name.Trim();
            var type = item.AssetType.Trim() switch
            {
                "Actor" => AssetTypeEnum.Actor,
                "角色" => AssetTypeEnum.Actor,
                "Scene" => AssetTypeEnum.Scene,
                "场景" => AssetTypeEnum.Scene,
                "Bgm" or "BGM" => AssetTypeEnum.Bgm,
                "Prop" => AssetTypeEnum.Prop,
                "道具" => AssetTypeEnum.Prop,
                "VoiceVoice" => AssetTypeEnum.VoiceVoice,
                "声音" => AssetTypeEnum.VoiceVoice,
                "Voice" => AssetTypeEnum.VoiceVoice,
                _ => AssetTypeEnum.Actor
            };

            if (existingByName.TryGetValue(name, out var existing))
            {
                if (item.Override)
                {
                    existing.Description = item.Description?.Trim() ?? existing.Description;
                    existing.AssetType = type;
                    toUpdate.Add(existing);
                }
                nameToAsset[name] = existing;
                continue;
            }

            var maxOrder = 0;
            var last = (await _assetService.GetByProjectAsync(req.ProjectId, type)).LastOrDefault();
            if (last != null) maxOrder = last.Order;
            var sameType = toAdd.Where(a => a.AssetType == type).ToList();
            if (sameType.Count > 0) maxOrder = Math.Max(maxOrder, sameType.Max(a => a.Order));

            var asset = new Asset
            {
                ProjectId = req.ProjectId,
                AssetType = type,
                Name = name,
                Description = item.Description?.Trim() ?? "",
                Order = maxOrder + 1
            };

            toAdd.Add(asset);
            nameToAsset[name] = asset;
        }

        // Pre-assign IDs to new assets so ParentId references work
        foreach (var asset in toAdd)
        {
            if (asset.Id == Guid.Empty)
                asset.Id = Guid.NewGuid();
        }

        foreach (var item in req.Assets)
        {
            var parentName = item.ParentName?.Trim();
            if (string.IsNullOrEmpty(parentName)) continue;
            if (!nameToAsset.TryGetValue(item.Name.Trim(), out var child)) continue;
            if (nameToAsset.TryGetValue(parentName, out var parent))
                child.ParentId = parent.Id;
        }

        var saved = 0;
        if (toAdd.Count > 0)
        {
            await _assetService.BulkCreateAsync(toAdd);
            saved += toAdd.Count;
        }
        foreach (var asset in toUpdate)
            await _assetService.UpdateAsync(asset);
        saved += toUpdate.Count;

        if (saved == 0)
            return Json(new { success = false, message = "没有需要操作的资产" });

        return Json(new { success = true, message = $"成功保存 {saved} 个资产（新增 {toAdd.Count}，更新 {toUpdate.Count}）", count = saved });
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
    public async Task<IActionResult> Edit()
    {
        string body;
        using (var reader = new StreamReader(Request.Body)) { body = await reader.ReadToEndAsync(); }
        body = body.Trim();
        if (string.IsNullOrEmpty(body))
            return Content(ToJson(false, "参数错误"));

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var idStr = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : "";
        if (!Guid.TryParse(idStr, out var id))
            return Content(ToJson(false, "参数错误"));

        var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "";
        var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : "";

        if (string.IsNullOrEmpty(name))
            return Content(ToJson(false, "名称不能为空"));

        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null)
            return Content(ToJson(false, "资产不存在"));

        asset.Name = name;
        asset.Description = description ?? "";

        var parentIdStr = root.TryGetProperty("parentId", out var pidProp) ? pidProp.GetString() : "";
        if (!string.IsNullOrEmpty(parentIdStr) && Guid.TryParse(parentIdStr, out var pid))
            asset.ParentId = pid;

        await _assetService.UpdateAsync(asset);
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

        var hasFileUpload = Request.Form.Files.Count > 0;

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
}

public class BulkCreateRequest
{
    public long ProjectId { get; set; }
    public List<BulkAssetItem> Assets { get; set; } = new();
}

public class BulkAssetItem
{
    public string Name { get; set; } = "";
    public string AssetType { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ParentName { get; set; }
    public bool Override { get; set; }
}