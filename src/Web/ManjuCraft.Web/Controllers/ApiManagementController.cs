using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

[Route("ApiManagement")]
public class ApiManagementController : Controller
{
    private readonly IProjectDbContext _db;

    public ApiManagementController(IProjectDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "API 管理";
        ViewBag.HideFooter = true;
        ViewBag.Providers = await _db.ApiProviders.OrderByDescending(p => p.Id).ToListAsync();
        return View();
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] ProviderRequest req)
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

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit([FromRoute] long id, [FromBody] ProviderRequest req)
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
        return Json(new { success = true });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromQuery] long id)
    {
        var existing = await _db.ApiProviders.FindAsync(id);
        if (existing == null) return Json(new { success = false, message = "不存在" });

        _db.ApiProviders.Remove(existing);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet("detail/{id}")]
    public async Task<IActionResult> Detail(long id)
    {
        var provider = await _db.ApiProviders.FindAsync(id);
        if (provider == null) return Json(new { success = false, message = "不存在" });
        return Json(new { success = true, data = provider });
    }

    private static AiCapability ParseCapability(string cap)
    {
        var upper = cap?.ToLowerInvariant() ?? "";
        if (upper.Contains("text")) return AiCapability.TextToText;
        if (upper.Contains("image")) return AiCapability.TextToImage;
        if (upper.Contains("audio")) return AiCapability.TextToAudio;
        if (upper.Contains("video")) return AiCapability.TextToVideo;
        if (upper.Contains("comfy")) return AiCapability.ImageToVideo;
        return AiCapability.TextToText;
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
