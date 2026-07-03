using Microsoft.AspNetCore.Mvc;

namespace ManjuCraft.Web.Controllers;

public class SettingsController : Controller
{

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
}