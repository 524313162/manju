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
            Type = ParseProviderType(dto.Type),
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
        existing.Type = ParseProviderType(dto.Type);
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
        var lower = cap?.Trim().ToLowerInvariant() ?? "";

        if (lower == "1" || lower == "texttotext" || lower.Contains("对话") || lower.Contains("llm")) return AiCapability.TextToText;
        if (lower == "2" || lower == "texttoimage" || lower.Contains("文生图") || lower.Contains("人物档案") || lower.Contains("图片生成")) return AiCapability.TextToImage;
        if (lower == "3" || lower == "texttoaudio" || lower.Contains("音频") || lower.Contains("music")) return AiCapability.TextToAudio;
        if (lower == "4" || lower == "texttovideo" || lower.Contains("文生视频")) return AiCapability.TextToVideo;
        if (lower == "5" || lower == "imagetovideo" || lower.Contains("图生视频")) return AiCapability.ImageToVideo;
        if (lower == "6" || lower == "texttoimage2") return AiCapability.TextToImage2;
        if (lower == "7" || lower == "texttomusic") return AiCapability.TextToMusic;
        if (lower == "8" || lower == "imagetoimage" || lower.Contains("图生图") || lower.Contains("人物档案") || lower.Contains("分镜")) return AiCapability.ImageToImage;
        if (lower == "9" || lower == "imagetoimageqwen" || lower.Contains("qwen")) return AiCapability.ImageToImageQwen;

        return AiCapability.TextToText;
    }

    private static ProviderType ParseProviderType(string type)
    {
        var upper = type?.ToLowerInvariant() ?? "";
        if (upper == "2" || upper.Contains("comfy")) return ProviderType.ComfyUI;
        return ProviderType.LLM;
    }
}
