using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Web.Controllers;

public class SettingsController : Controller
{
    private readonly IComfyuiConnectionService _connectionService;

    public SettingsController(IComfyuiConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "设置";
        ViewBag.HideFooter = true;
        var apiUrl = Environment.GetEnvironmentVariable("Comfyui__ApiUrl") ?? "http://localhost:8188";
        var wsUrl = Environment.GetEnvironmentVariable("Comfyui__WsUrl") ?? "ws://localhost:8188/ws";
        ViewBag.ComfyuiApiUrl = apiUrl;
        ViewBag.ComfyuiWsUrl = wsUrl;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TestComfyuiConnection(string apiUrl)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            apiUrl = Environment.GetEnvironmentVariable("Comfyui__ApiUrl") ?? "http://localhost:8188";

        var status = await _connectionService.TestConnectionAsync(apiUrl);
        return Json(new { success = status.IsConnected, message = status.IsConnected ? "连接成功" : status.ErrorMessage, data = status });
    }
}