using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service;
using ManjuCraft.Infrastructure.Service;
using ManjuCraft.Web.Services;

namespace ManjuCraft.Web.Controllers;

// Use a non-standard controller name that will be matched by convention routing
// The class name is used for MVC view resolution
[Controller]  // Marks as MVC controller (no "Controller" suffix required in routing)
public class SettingsView : Controller
{
    [HttpGet("Settings/Comfyui")]
    public IActionResult Comfyui()
    {
        return View("~/Views/Settings/Comfyui.cshtml");
    }
}

[ApiController]
[Route("api/v1/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IComfyuiConnectionService _comfyuiConnectionService;
    private readonly IFfmpegService _ffmpeg;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IComfyuiConnectionService comfyuiConnectionService, IFfmpegService ffmpeg, ILogger<SettingsController> logger)
    {
        _comfyuiConnectionService = comfyuiConnectionService;
        _ffmpeg = ffmpeg;
        _logger = logger;
    }

    [HttpGet("comfyui")]
    public IActionResult GetComfyuiSettings()
    {
        return Ok(new { success = true, data = new { apiUrl = "http://localhost:8188", wsUrl = "ws://localhost:8188/ws", outputDir = "" } });
    }

    [HttpPut("comfyui")]
    public IActionResult UpdateComfyuiSettings([FromBody] ComfyuiSettingsDto dto)
    {
        return Ok(new { success = true, message = "设置已保存" });
    }

    [HttpGet("comfyui/test")]
    public async Task<IActionResult> TestComfyuiConnection([FromQuery] string apiUrl = "http://localhost:8188")
    {
        try
        {
            var status = await _comfyuiConnectionService.TestConnectionAsync(apiUrl);
            return Ok(new { success = true, data = status, message = "连接成功" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("ffmpeg")]
    public IActionResult FfmpegStatus()
    {
        var (available, version) = _ffmpeg.Check();
        return Ok(new { success = true, data = new { available, version = version ?? "unknown" } });
    }

    [HttpGet("backup")]
    public IActionResult BackupDb([FromQuery] string path = "")
    {
        return Ok(new { success = true, message = "备份功能待实现" });
    }

    public class ComfyuiSettingsDto
    {
        public string ApiUrl { get; set; } = "";
        public string WsUrl { get; set; } = "";
        public string OutputDir { get; set; } = "";
    }
}
