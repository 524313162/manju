using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Web.Services;
using ManjuCraft.Infrastructure.Service;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ManjuCraft.Web.Controllers;

[Route("Export")]
[Route("Export/{action=Index}")]
public class ExportViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Export/Index.cshtml");
    }
}

[Route("api/v1/export")]
[ApiController]
public class ExportController : ControllerBase
{
    private readonly IEpisodeService _episodeService;
    private readonly IShotService _shotService;
    private readonly IFfmpegService _ffmpeg;
    private readonly IProjectService _projectService;
    private readonly IFileStorageService _fileStorage;
    private readonly ProjectDbContext _dbContext;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IEpisodeService episodeService,
        IShotService shotService,
        IFfmpegService ffmpeg,
        IProjectService projectService,
        IFileStorageService fileStorage,
        ProjectDbContext dbContext,
        ILogger<ExportController> logger)
    {
        _episodeService = episodeService;
        _shotService = shotService;
        _ffmpeg = ffmpeg;
        _projectService = projectService;
        _fileStorage = fileStorage;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("ffmpeg-status")]
    public IActionResult CheckFfmpeg()
    {
        var (available, version) = _ffmpeg.Check();
        return Ok(new
        {
            success = true,
            data = new { available, version = version ?? "unknown" }
        });
    }

    [HttpPost("{projectId}/merge")]
    public async Task<IActionResult> MergeProjectVideos(long projectId)
    {
        try
        {
            _ffmpeg.Check();

            var episodes = await _episodeService.GetByProjectAsync(projectId);
            if (!episodes.Any())
                return BadRequest(new { success = false, message = "该项目没有分集" });

            var allShots = new List<(Shot Shot, Episode Episode)>();
            foreach (var ep in episodes)
            {
                var shots = await _shotService.GetByEpisodeAsync(ep.Id);
                foreach (var shot in shots)
                    allShots.Add((shot, ep));
            }

            if (!allShots.Any())
                return BadRequest(new { success = false, message = "该项目没有分镜" });

            var project = await _projectService.GetByIdAsync(projectId);
            if (project == null) return NotFound(new { success = false, message = "项目不存在" });

            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", projectId.ToString(), "export");
            Directory.CreateDirectory(outputDir);

            var videoFiles = new List<string>();
            var tempFiles = new List<string>();

            foreach (var (shot, ep) in allShots)
            {
                var videoUrl = _fileStorage.GetAssetUrl(projectId, "shot", shot.Id, "Video");
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/'), videoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        var tempFile = Path.Combine(outputDir, $"shot-{shot.Id}.mp4");
                        System.IO.File.Copy(fullPath, tempFile, true);
                        videoFiles.Add(tempFile);
                        tempFiles.Add(tempFile);
                    }
                }
            }

            if (!videoFiles.Any())
                return BadRequest(new { success = false, message = "没有可用的视频片段，请先分镜视频" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    status = "processing",
                    message = $"正在合并 {videoFiles.Count} 个视频片段",
                    totalSegments = videoFiles.Count,
                    currentSegment = 0
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "视频合并任务启动失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{projectId}/merge/run")]
    public async Task<IActionResult> RunMerge(long projectId)
    {
        try
        {
            var episodes = await _episodeService.GetByProjectAsync(projectId);
            var allShots = new List<Shot>();
            foreach (var ep in episodes)
            {
                var shots = await _shotService.GetByEpisodeAsync(ep.Id);
                allShots.AddRange(shots);
            }

            var project = await _projectService.GetByIdAsync(projectId);
            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", projectId.ToString(), "export");
            Directory.CreateDirectory(outputDir);

            var videoFiles = new List<string>();

            foreach (var shot in allShots)
            {
                var videoUrl = _fileStorage.GetAssetUrl(projectId, "shot", shot.Id, "Video");
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/'), videoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        videoFiles.Add(fullPath);
                    }
                }
            }

            if (!videoFiles.Any())
                return BadRequest(new { success = false, message = "没有可用的视频片段" });

            var outputPath = Path.Combine(outputDir, $"manju-{projectId}-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");

            var result = await _ffmpeg.MergeVideosAsync(videoFiles.ToArray(), outputPath, []);

            if (result.Success)
            {
                var relativePath = outputPath.Replace(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/'), "");
                relativePath = relativePath.Replace('\\', '/');
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        status = "completed",
                        fileUrl = $"/{relativePath}",
                        fileName = Path.GetFileName(outputPath)
                    }
                });
            }

            return Ok(new { success = false, message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "视频合并执行失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}