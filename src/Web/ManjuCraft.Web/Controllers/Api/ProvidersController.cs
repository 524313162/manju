using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers.Api;

[Route("api/v1/providers")]
[ApiController]
public class ProvidersController : ControllerBase
{
    private readonly IProjectDbContext _db;

    public ProvidersController(IProjectDbContext db)
    {
        _db = db;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var providers = await _db.ApiProviders
            .OrderByDescending(p => p.Id)
            .ToListAsync();
        return Ok(new { success = true, data = providers });
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
        return Ok(new { success = true, data = provider });
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(long id, [FromBody] ProviderRequest req)
    {
        var existing = await _db.ApiProviders.FindAsync(id);
        if (existing == null) return Ok(new { success = false, message = "不存在" });

        existing.Name = req.Name;
        existing.Capability = ParseCapability(req.Capability);
        existing.ApiUrl = req.ApiUrl;
        existing.ApiKey = req.ApiKey;
        existing.Model = req.Model;
        existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = existing });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest req)
    {
        var existing = await _db.ApiProviders.FindAsync(req.Id);
        if (existing == null) return Ok(new { success = false, message = "不存在" });

        _db.ApiProviders.Remove(existing);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test(long id)
    {
        var provider = await _db.ApiProviders.FindAsync(id);
        if (provider == null) return Ok(new { success = false, message = "提供者不存在" });

        try
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(provider.ApiUrl.TrimEnd('/')), Timeout = TimeSpan.FromSeconds(10) };
            var req2 = new HttpRequestMessage(HttpMethod.Post, "/chat/completions");
            req2.Content = System.Net.Http.Json.JsonContent.Create(new { model = "test", messages = new[] { new { role = "system", content = "test" } }, stream = false });
            if (!string.IsNullOrEmpty(provider.ApiKey))
                req2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);
            var res = await httpClient.SendAsync(req2);
            return Ok(new { success = res.IsSuccessStatusCode, message = res.IsSuccessStatusCode ? "\u8fde\u63a5\u6210\u529f" : $"State: {res.StatusCode}" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
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
        public string Name { get; set; } = default!;
        public string Capability { get; set; } = default!;
        public string ApiUrl { get; set; } = default!;
        public string ApiKey { get; set; } = default!;
        public string Model { get; set; } = default!;
    }

    public class DeleteRequest
    {
        public long Id { get; set; }
    }
}
