using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Web.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class AssetGenerationController : ControllerBase
{
    private readonly IComfyuiClient _comfyuiClient;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<AssetGenerationController> _logger;

    public AssetGenerationController(
        IComfyuiClient comfyuiClient,
        IFileStorageService fileStorageService,
        ILogger<AssetGenerationController> logger)
    {
        _comfyuiClient = comfyuiClient;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    [HttpPost("{assetId}/generate-images")]
    public async Task<IActionResult> GenerateImages(long assetId, [FromForm] string? workflowType = null)
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var asset = ctx.Assets.Include(a => a.Project).SingleOrDefault(a => a.Id == assetId);
            if (asset == null) return NotFound(new { success = false, message = "资产不存在" });

            var apiUrl = "http://localhost:8188";
            var workflowJson = "{\"prompt\": \"" + (asset.Description ?? "").Replace("\"", "\\\"") + "\", \"assetType\": \"" + asset.AssetType + "\"}";

            await _comfyuiClient.SubmitPromptAsync(apiUrl, workflowJson, asset.ProjectId);

            return Ok(new { success = true, data = new { taskId = Guid.NewGuid().ToString(), status = "processing", message = "生成任务已提交" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "资产图片生成失败: {AssetId}", assetId);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{assetId}/images/upload")]
    public async Task<IActionResult> UploadImage(long assetId, IFormFile file, [FromForm] string viewType = "Front")
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var asset = ctx.Assets.SingleOrDefault(a => a.Id == assetId);
            if (asset == null) return NotFound(new { success = false, message = "资产不存在" });

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "请选择要上传的文件" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName) ?? ".png";

            var url = await _fileStorageService.SaveAssetAsync(
                asset.ProjectId, asset.AssetType.ToLower(), asset.Id, viewType, ms.ToArray(), ext);

            return Ok(new { success = true, data = new { fileUrl = url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "资产图片上传失败: {AssetId}", assetId);
            return Ok(new { success = false, message = ex.Message });
        }
    }
}