using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Web.Controllers;

// MVC route for Shots pages
[Route("Shots")]
[Route("Shots/{action=Index}")]
public class ShotsViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Shots/Index.cshtml");
    }
}

[Route("api/v1/shots")]
[ApiController]
public class ShotGenerationController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;
    private readonly IComfyuiClient _comfyuiClient;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ShotGenerationController> _logger;

    public ShotGenerationController(
        ProjectDbContext dbContext,
        IComfyuiClient comfyuiClient,
        IFileStorageService fileStorageService,
        ILogger<ShotGenerationController> logger)
    {
        _dbContext = dbContext;
        _comfyuiClient = comfyuiClient;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    [HttpPost("{id}/generate-firstframe")]
    public async Task<IActionResult> GenerateFirstFrame(long id, [FromForm] string? workflowType = null)
    {
        try
        {
            var shot = _dbContext.Shots.Include(s => s.Episode).ThenInclude(e => e.Project).FirstOrDefault(s => s.Id == id);
            if (shot == null) return NotFound(new { success = false, message = "分镜不存在" });

            var project = shot.Episode.Project;
            var apiUrl = project?.GetComfyuiConfig()?.GetValueOrDefault("apiUrl") ?? "http://localhost:8188";
            var wfType = workflowType ?? shot.FirstFrameWorkflowType ?? "Img2Img";
            var workflowJson = "{\"prompt\": \"" + (shot.FirstFramePrompt ?? "").Replace("\"", "\\\"") + "\", \"workflowType\": \"" + wfType + "\"}";

            await _comfyuiClient.SubmitPromptAsync(apiUrl, workflowJson, shot.Episode.ProjectId);

            return Ok(new { success = true, data = new { taskId = Guid.NewGuid().ToString(), status = "pending", message = "首帧图生成任务已提交" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "首帧图生成失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/generate-video")]
    public async Task<IActionResult> GenerateVideo(long id, [FromForm] string? workflowType = null)
    {
        try
        {
            var shot = _dbContext.Shots.Include(s => s.Episode).ThenInclude(e => e.Project).FirstOrDefault(s => s.Id == id);
            if (shot == null) return NotFound(new { success = false, message = "分镜不存在" });

            var wfType = workflowType ?? shot.VideoWorkflowType ?? "Img2Video";
            var project = shot.Episode.Project;
            var apiUrl = project?.GetComfyuiConfig()?.GetValueOrDefault("apiUrl") ?? "http://localhost:8188";
            var workflowJson = "{\"prompt\": \"" + (shot.VideoPrompt ?? "").Replace("\"", "\\\"") + "\", \"workflowType\": \"" + wfType + "\"}";

            await _comfyuiClient.SubmitPromptAsync(apiUrl, workflowJson, shot.Episode.ProjectId);

            return Ok(new { success = true, data = new { taskId = Guid.NewGuid().ToString(), status = "pending", message = "镜头视频生成任务已提交" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "镜头视频生成失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/images/upload")]
    public async Task<IActionResult> UploadFirstFrame(long id, IFormFile file, [FromForm] string viewType = "FirstFrame")
    {
        try
        {
            var shot = _dbContext.Shots.Include(s => s.Episode).First(s => s.Id == id);
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "请选择要上传的文件" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName) ?? ".png";

            var url = await _fileStorageService.SaveAssetAsync(
                shot.Episode.ProjectId, "shot", shot.Id, viewType, ms.ToArray(), ext);

            return Ok(new { success = true, data = new { fileUrl = url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "首帧图上传失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/video/upload")]
    public async Task<IActionResult> UploadVideo(long id, IFormFile file, [FromForm] string viewType = "Video")
    {
        try
        {
            var shot = _dbContext.Shots.Include(s => s.Episode).First(s => s.Id == id);
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "请选择要上传的文件" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName) ?? ".mp4";

            var url = await _fileStorageService.SaveAssetAsync(
                shot.Episode.ProjectId, "shot", shot.Id, viewType, ms.ToArray(), ext);

            return Ok(new { success = true, data = new { fileUrl = url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "视频上传失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}
