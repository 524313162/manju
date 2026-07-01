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

    [HttpPost("actors/{actorId}/generate-images")]
    public async Task<IActionResult> GenerateActorImages(long actorId, [FromForm] string? workflowType = null)
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var actor = ctx.Actors.Include(a => a.Project).SingleOrDefault(a => a.Id == actorId);
            if (actor == null) return NotFound(new { success = false, message = "演员不存在" });

            var wfType = workflowType ?? actor.DefaultWorkflowType ?? "Txt2Img";
            var apiUrl = "http://localhost:8188";
            var workflowJson = "{\"prompt\": \"" + (actor.FourViewPrompt ?? "").Replace("\"", "\\\"") + "\"}";

            await _comfyuiClient.SubmitPromptAsync(apiUrl, workflowJson, actor.ProjectId);

            return Ok(new { success = true, data = new { taskId = Guid.NewGuid().ToString(), status = "processing", message = "生成任务已提交" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "演员图片生成失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("actors/{actorId}/images/upload")]
    public async Task<IActionResult> UploadActorImage(long actorId, IFormFile file, [FromForm] string viewType = "Front")
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var actor = ctx.Actors.SingleOrDefault(a => a.Id == actorId);
            if (actor == null) return NotFound(new { success = false, message = "演员不存在" });

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "请选择要上传的文件" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName) ?? ".png";

            var url = await _fileStorageService.SaveAssetAsync(
                actor.ProjectId, "actor", actor.Id, viewType, ms.ToArray(), ext);

            return Ok(new { success = true, data = new { fileUrl = url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "演员图片上传失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("props/{propId}/generate-images")]
    public async Task<IActionResult> GeneratePropImages(long propId, [FromForm] string? workflowType = null)
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var prop = ctx.Props.Single(p => p.Id == propId);

            var wfType = workflowType ?? prop.DefaultWorkflowType ?? "Txt2Img";
            var apiUrl = "http://localhost:8188";
            var workflowJson = "{\"prompt\": \"\"}";
            await _comfyuiClient.SubmitPromptAsync(apiUrl, workflowJson, prop.ProjectId);

            return Ok(new { success = true, data = new { taskId = Guid.NewGuid().ToString(), status = "processing", message = "生成任务已提交" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "道具图片生成失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("props/{propId}/images/upload")]
    public async Task<IActionResult> UploadPropImage(long propId, IFormFile file, [FromForm] string viewType = "Front")
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var prop = ctx.Props.Single(p => p.Id == propId);

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "请选择要上传的文件" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName) ?? ".png";

            var url = await _fileStorageService.SaveAssetAsync(
                prop.ProjectId, "prop", prop.Id, viewType, ms.ToArray(), ext);

            return Ok(new { success = true, data = new { fileUrl = url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "道具图片上传失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("scenes/{sceneId}/generate-images")]
    public async Task<IActionResult> GenerateSceneImages(long sceneId, [FromForm] string? workflowType = null)
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var scene = ctx.Scenes.Single(s => s.Id == sceneId);

            var wfType = workflowType ?? scene.DefaultWorkflowType ?? "Txt2Img";
            var apiUrl = "http://localhost:8188";
            var workflowJson = "{\"prompt\": \"\"}";
            await _comfyuiClient.SubmitPromptAsync(apiUrl, workflowJson, scene.ProjectId);

            return Ok(new { success = true, data = new { taskId = Guid.NewGuid().ToString(), status = "processing", message = "生成任务已提交" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "场景图片生成失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("scenes/{sceneId}/images/upload")]
    public async Task<IActionResult> UploadSceneImage(long sceneId, IFormFile file, [FromForm] string viewType = "Main")
    {
        try
        {
            using var ctx = new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>()
                .UseSqlite("Data Source=manju.db").Options);
            var scene = ctx.Scenes.Single(s => s.Id == sceneId);

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "请选择要上传的文件" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName) ?? ".png";

            var url = await _fileStorageService.SaveAssetAsync(
                scene.ProjectId, "scene", scene.Id, viewType, ms.ToArray(), ext);

            return Ok(new { success = true, data = new { fileUrl = url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "场景图片上传失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}
