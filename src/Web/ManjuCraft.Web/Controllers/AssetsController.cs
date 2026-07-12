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
}

public class GenerateCharacterImageRequest
{
    public string? SystemPrompt { get; set; }
    public string CharacterPrompt { get; set; } = "";
    public string? NegativePrompt { get; set; }
    public int Width { get; set; } = 1792;
    public int Height { get; set; } = 1024;
}
