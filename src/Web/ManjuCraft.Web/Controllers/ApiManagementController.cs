using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service.Dtos;
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
    public async Task<IActionResult> Add([FromBody] ProviderDto dto)
    {
        var provider = new ApiProvider
        {
            Name = dto.Name,
            Capability = ParseCapability(dto.Capability),
            ApiUrl = dto.ApiUrl,
            ApiKey = dto.ApiKey,
            Model = dto.Model
        };
        _db.ApiProviders.Add(provider);
        await _db.SaveChangesAsync();
        return Json(new { success = true, data = provider });
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit([FromRoute] long id, [FromBody] ProviderDto dto)
    {
        var existing = await _db.ApiProviders.FindAsync(id);
        if (existing == null) return Json(new { success = false, message = "不存在" });

        existing.Name = dto.Name;
        existing.Capability = ParseCapability(dto.Capability);
        existing.ApiUrl = dto.ApiUrl;
        existing.ApiKey = dto.ApiKey;
        existing.Model = dto.Model;
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
}
