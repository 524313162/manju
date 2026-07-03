using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Web.Controllers;

[Route("Shots")]
[Route("Shots/{action=Index}")]
public class ShotsViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        var episodeId = Request.Query["episodeId"].ToString();
        if (!string.IsNullOrEmpty(episodeId))
            ViewData["EpisodeId"] = episodeId;
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

    [HttpGet("{id}")]
    public IActionResult GetById(long id)
    {
        var shot = _dbContext.Shots
            .Include(s => s.Frames.OrderBy(f => f.Order))
            .FirstOrDefault(s => s.Id == id);
        if (shot == null) return NotFound();
        return Ok(new { success = true, data = shot });
    }

    [HttpGet("episode/{episodeId}")]
    public IActionResult ListByEpisode(long episodeId)
    {
        var shots = _dbContext.Shots
            .Where(s => s.EpisodeId == episodeId)
            .OrderBy(s => s.Order)
            .Select(s => new {
                s.Id, s.EpisodeId, s.AssetRefs, s.Description, s.ShotSize, s.CameraMovement, s.Duration, s.Order,
                FrameCount = _dbContext.ShotFrames.Count(f => f.ShotId == s.Id),
                s.CreatedTime, s.UpdatedTime
            })
            .ToList();
        return Ok(new { success = true, data = shots });
    }

    [HttpPost("episode/{episodeId}")]
    public IActionResult Create(long episodeId, [FromBody] ShotDto dto)
    {
        var shot = new Shot
        {
            EpisodeId = episodeId,
            AssetRefs = dto.AssetRefs ?? "{}",
            Description = dto.Description,
            ShotSize = dto.ShotSize ?? "",
            CameraMovement = dto.CameraMovement ?? "",
            Duration = dto.Duration,
            Order = dto.Order,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _dbContext.Shots.Add(shot);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { shot.Id, shot.EpisodeId } });
    }

    [HttpPut("{id}")]
    public IActionResult Update(long id, [FromBody] ShotDto dto)
    {
        var shot = _dbContext.Shots.Find(id);
        if (shot == null) return NotFound();
        shot.AssetRefs = dto.AssetRefs ?? "{}";
        shot.Description = dto.Description;
        shot.ShotSize = dto.ShotSize ?? "";
        shot.CameraMovement = dto.CameraMovement ?? "";
        shot.Duration = dto.Duration;
        shot.Order = dto.Order;
        shot.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(long id)
    {
        var shot = _dbContext.Shots.Find(id);
        if (shot == null) return NotFound();
        _dbContext.Shots.Remove(shot);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPut("reorder")]
    public IActionResult Reorder([FromBody] ReorderShotsDto dto)
    {
        for (int i = 0; i < dto.ShotIds.Length; i++)
        {
            var shot = _dbContext.Shots.Find(dto.ShotIds[i]);
            if (shot != null)
            {
                shot.Order = i;
                shot.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    [HttpGet("{shotId}/frames")]
    public IActionResult GetFrames(long shotId)
    {
        var frames = _dbContext.ShotFrames
            .Where(f => f.ShotId == shotId)
            .OrderBy(f => f.Order)
            .Select(f => new { f.Id, f.ShotId, f.ProjectId, f.FrameType, f.Description, f.ResourceId, f.StartTime, f.Duration, f.Order })
            .ToList();
        return Ok(new { success = true, data = frames });
    }

    [HttpPost("{shotId}/frames")]
    public IActionResult CreateFrame(long shotId, [FromBody] ShotFrameDto dto)
    {
        var shot = _dbContext.Shots.Include(s => s.Episode).FirstOrDefault(s => s.Id == shotId);
        if (shot == null) return NotFound();

        var frame = new ShotFrame
        {
            ShotId = shotId,
            ProjectId = shot.Episode.ProjectId,
            FrameType = dto.FrameType,
            Description = dto.Description,
            ResourceId = dto.ResourceId,
            StartTime = dto.StartTime,
            Duration = dto.Duration,
            Order = dto.Order,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _dbContext.ShotFrames.Add(frame);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { frame.Id, frame.FrameType } });
    }

    [HttpPut("frames/{frameId}")]
    public IActionResult UpdateFrame(long frameId, [FromBody] ShotFrameDto dto)
    {
        var frame = _dbContext.ShotFrames.Find(frameId);
        if (frame == null) return NotFound();
        frame.FrameType = dto.FrameType;
        frame.Description = dto.Description;
        frame.ResourceId = dto.ResourceId;
        frame.StartTime = dto.StartTime;
        frame.Duration = dto.Duration;
        frame.Order = dto.Order;
        frame.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("frames/{frameId}")]
    public IActionResult DeleteFrame(long frameId)
    {
        var frame = _dbContext.ShotFrames.Find(frameId);
        if (frame == null) return NotFound();
        _dbContext.ShotFrames.Remove(frame);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPost("{id}/generate-firstframe")]
    public async Task<IActionResult> GenerateFirstFrame(long id, [FromForm] string? prompt = null)
    {
        try
        {
            var shot = _dbContext.Shots.Include(s => s.Episode).FirstOrDefault(s => s.Id == id);
            if (shot == null) return NotFound(new { success = false, message = "分镜不存在" });

            var apiUrl = "http://localhost:8188";
            var genPrompt = prompt ?? shot.Description;
            var workflowJson = "{\"prompt\": \"" + (genPrompt ?? "").Replace("\"", "\\\"") + "\"}";

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
    public async Task<IActionResult> GenerateVideo(long id, [FromForm] string? prompt = null)
    {
        try
        {
            var shot = _dbContext.Shots.Include(s => s.Episode).FirstOrDefault(s => s.Id == id);
            if (shot == null) return NotFound(new { success = false, message = "分镜不存在" });

            var apiUrl = "http://localhost:8188";
            var genPrompt = prompt ?? shot.Description;
            var workflowJson = "{\"prompt\": \"" + (genPrompt ?? "").Replace("\"", "\\\"") + "\", \"workflowType\": \"Img2Video\"}";

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
    public async Task<IActionResult> UploadImage(long id, IFormFile file, [FromForm] string viewType = "FirstFrame")
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
            _logger.LogError(ex, "图片上传失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/video/upload")]
    public async Task<IActionResult> UploadVideo(long id, IFormFile file)
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
                shot.Episode.ProjectId, "shot", shot.Id, "Video", ms.ToArray(), ext);

            return Ok(new { success = true, data = new { fileUrl = url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "视频上传失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    public class ShotDto
    {
        public string? AssetRefs { get; set; }
        public string Description { get; set; } = "";
        public string? ShotSize { get; set; }
        public string? CameraMovement { get; set; }
        public float? Duration { get; set; }
        public int Order { get; set; }
    }

    public class ShotFrameDto
    {
        public string FrameType { get; set; } = "";
        public string Description { get; set; } = "";
        public long? ResourceId { get; set; }
        public float? StartTime { get; set; }
        public float? Duration { get; set; }
        public int Order { get; set; }
    }

    public class ReorderShotsDto
    {
        public long[] ShotIds { get; set; } = [];
    }
}