using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using ManjuCraft.Web.Services;

namespace ManjuCraft.Web.Controllers;

public class SettingsController : Controller
{
    private readonly IProjectDbContext _db;
    private readonly ComfyuiProxyApi _comfyuiApi;

    public SettingsController(IProjectDbContext db, ComfyuiProxyApi comfyuiApi)
    {
        _db = db;
        _comfyuiApi = comfyuiApi;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "设置";
        ViewBag.HideFooter = true;

        var apiUrl = Environment.GetEnvironmentVariable("Comfyui__ApiUrl") ?? "http://localhost:8188";
        var wsUrl = Environment.GetEnvironmentVariable("Comfyui__WsUrl") ?? "ws://localhost:8188/ws";
        ViewBag.ComfyuiApiUrl = apiUrl;
        ViewBag.ComfyuiWsUrl = wsUrl;

        ViewBag.Providers = await _db.ApiProviders
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        ViewBag.Templates = await _db.PromptTemplates
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddProvider([FromBody] ProviderRequest req)
    {
        var provider = new ApiProvider
        {
            Name = req.Name,
            Capability = ParseCapability(req.Capability),
            ApiUrl = req.ApiUrl,
            ApiKey = req.ApiKey,
            Model = req.Model
        };

        _db.ApiProviders.Add(provider);
        await _db.SaveChangesAsync();
        return Json(new { success = true, data = provider });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProvider(long id, [FromBody] ProviderRequest req)
    {
        var existing = await _db.ApiProviders.FindAsync(id);
        if (existing == null) return Json(new { success = false, message = "不存在" });

        existing.Name = req.Name;
        existing.Capability = ParseCapability(req.Capability);
        existing.ApiUrl = req.ApiUrl;
        existing.ApiKey = req.ApiKey;
        existing.Model = req.Model;
        existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await _db.SaveChangesAsync();
        return Json(new { success = true, data = existing });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProvider(long id)
    {
        var existing = await _db.ApiProviders.FindAsync(id);
        if (existing == null) return Json(new { success = false, message = "不存在" });

        _db.ApiProviders.Remove(existing);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> TestProviderConnection(long id)
    {
        var provider = await _db.ApiProviders.FindAsync(id);
        if (provider == null) return Json(new { success = false, message = "提供者不存在" });

        if (provider.Capability == AiCapability.TextToText)
        {
            try
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(provider.ApiUrl.TrimEnd('/')),
                    Timeout = TimeSpan.FromSeconds(10)
                };
                var req = new HttpRequestMessage(HttpMethod.Post, "/chat/completions");
                req.Content = System.Net.Http.Json.JsonContent.Create(new { model = "test", messages = new[] { new { role = "system", content = "test" } }, stream = false });
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);
                var res = await httpClient.SendAsync(req);
                return Json(new { success = res.IsSuccessStatusCode, message = res.IsSuccessStatusCode ? "连接成功" : $"状态码: {res.StatusCode}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        if (provider.Capability == AiCapability.TextToVideo)
        {
            var health = await _comfyuiApi.CheckHealthAsync();
            return Json(new { success = health, message = health ? "连接成功" : "连接失败" });
        }

        return Json(new { success = false, message = "不支持的能力类型" });
    }

    private static Domain.Models.AiCapability ParseCapability(string cap)
    {
        var upper = cap?.ToLowerInvariant() ?? "";
        if (upper.Contains("text")) return Domain.Models.AiCapability.TextToText;
        if (upper.Contains("image")) return Domain.Models.AiCapability.TextToImage;
        if (upper.Contains("audio")) return Domain.Models.AiCapability.TextToAudio;
        if (upper.Contains("video")) return Domain.Models.AiCapability.TextToVideo;
        if (upper.Contains("comfy")) return Domain.Models.AiCapability.ImageToVideo;

        return Domain.Models.AiCapability.TextToText;
    }

    public class ProviderRequest
    {
        public string Name { get; set; }
        public string Capability { get; set; }
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
    }
}