using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace ManjuCraft.Web.Controllers;

public class AssetsController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IProjectService _projectService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWebHostEnvironment _env;

    public AssetsController(IAssetService assetService, IProjectService projectService, IFileStorageService fileStorageService, IWebHostEnvironment env)
    {
        _assetService = assetService;
        _projectService = projectService;
        _fileStorageService = fileStorageService;
        _env = env;
    }

    public async Task<IActionResult> Index(long projectId, AssetTypeEnum type = AssetTypeEnum.Actor)
    {
        if (projectId <= 0)
        {
            return RedirectToAction("Index", "Projects");
        }

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

    [HttpPost]
    public async Task<IActionResult> Create(long? projectId, AssetTypeEnum? assetType, string? name, string? description, int order = 0)
    {
        if (string.IsNullOrEmpty(name))
            return Content(ToJson(false, "名称不能为空"));

        var asset = new Asset
        {
            ProjectId = projectId ?? 0,
            AssetType = assetType ?? AssetTypeEnum.Actor,
            Name = name,
            Description = description ?? String.Empty,
            Order = order
        };
        await _assetService.CreateAsync(asset);
        return Content(ToJson(true));
    }

    [HttpPost]
    public async Task<IActionResult> Edit()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }
        body = body.Trim();
        if (string.IsNullOrEmpty(body))
            return Content(ToJson(false, "参数错误"));

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0L;
        var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "";
        var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : "";
        var order = root.TryGetProperty("order", out var orderProp) ? orderProp.GetInt32() : 0;

        if (id <= 0)
            return Content(ToJson(false, "参数错误"));
        if (string.IsNullOrEmpty(name))
            return Content(ToJson(false, "名称不能为空"));

        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null)
            return Content(ToJson(false, "资产不存在"));

        asset.Name = name;
        asset.Description = description ?? String.Empty;
        asset.Order = order;
        await _assetService.UpdateAsync(asset);
        return Content(ToJson(true));
    }

    [HttpPost]
    public async Task<IActionResult> Delete()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }
        body = body.Trim();
        if (string.IsNullOrEmpty(body))
            return Content(ToJson(false, "参数错误"));

        using var doc = JsonDocument.Parse(body);
        var id = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0L;

        if (id <= 0)
            return Content(ToJson(false, "参数错误"));

        await DeleteResourceForAssetAsync(id);
        await _assetService.DeleteAsync(id);
        return Content(ToJson(true));
    }

    [HttpPost]
    public async Task<IActionResult> ReplaceResource()
    {
        long? projectId = null;
        long assetId = 0;
        string fileUrl = null;

        var hasFileUpload = Request.Form.Files.Count > 0;

        if (!hasFileUpload)
        {
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }
            body = body.Trim();
            if (string.IsNullOrEmpty(body))
                return Content(ToJson(false, "参数错误"));

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            assetId = root.TryGetProperty("assetId", out var idProp) ? idProp.GetInt64() : 0L;
            fileUrl = root.TryGetProperty("fileUrl", out var urlProp) ? urlProp.GetString() : null;
        }

        if (assetId <= 0)
            return Content(ToJson(false, "参数错误"));

        var asset = await _assetService.GetByIdAsync(assetId);
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
        else
        {
            return Content(ToJson(false, "请提供文件"));
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReplaceAudio()
    {
        await ReplaceResourceAsync();
        return Content(ToJson(true));
    }

    private async Task ReplaceResourceAsync()
    {
        long assetId = 0;
        string fileUrl = null;

        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }
        body = body.Trim();
        if (string.IsNullOrEmpty(body))
            return;

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        assetId = root.TryGetProperty("assetId", out var idProp) ? idProp.GetInt64() : 0L;
        fileUrl = root.TryGetProperty("fileUrl", out var urlProp) ? urlProp.GetString() : null;

        if (assetId <= 0 || string.IsNullOrEmpty(fileUrl))
            return;

        var asset = await _assetService.GetByIdAsync(assetId);
        if (asset == null)
            return;

        await SaveResourceFromUrlAsync(asset, fileUrl);
    }

    private async Task SaveResourceFromUrlAsync(Asset asset, string fileUrl)
    {
        await DeleteResourceForAssetAsync(asset.Id);

        var ext = System.IO.Path.GetExtension(new Uri(fileUrl).AbsolutePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
        {
            var contentType = "text/plain";
            switch (asset.AssetType)
            {
                case AssetTypeEnum.Actor:
                case AssetTypeEnum.Scene:
                case AssetTypeEnum.Prop:
                    contentType = "image/png";
                    ext = ".png";
                    break;
                case AssetTypeEnum.Bgm:
                    contentType = "audio/mpeg";
                    ext = ".mp3";
                    break;
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

    private async Task DeleteResourceForAssetAsync(long assetId)
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
        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!string.IsNullOrEmpty(ext))
            return ext;
        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        switch (contentType)
        {
            // Images
            case "image/png": return ".png";
            case "image/jpeg":
            case "image/jpg": return ".jpg";
            case "image/webp": return ".webp";
            case "image/gif": return ".gif";
            case "image/bmp": return ".bmp";
            case "image/svg+xml": return ".svg";
            // Audio
            case "audio/mpeg": return ".mp3";
            case "audio/wav": return ".wav";
            case "audio/ogg": return ".ogg";
            case "audio/mp4":
            case "audio/aac": return ".m4a";
            default: return ext;
        }
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
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }
}
