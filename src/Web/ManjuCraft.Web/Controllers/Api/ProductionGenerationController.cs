using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using ManjuCraft.Application.AI;

namespace ManjuCraft.Web.Controllers.Api;

[ApiController]
[Route("api/v1/production")]
public class ProductionGenerationController : ControllerBase
{
    private readonly IAiAgentService _aiAgent;
    private readonly IProjectDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductionGenerationController(IAiAgentService aiAgent, IProjectDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _aiAgent = aiAgent;
        _db = db;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("generate-video")]
    public async Task<IActionResult> GenerateVideo(string prompt, long? providerId = null)
    {
        var provider = providerId.HasValue
            ? await _db.ApiProviders.FirstOrDefaultAsync(p => p.Id == providerId.Value)
            : await _db.ApiProviders.Where(p => p.Capability == AiCapability.TextToVideo)
                .OrderByDescending(p => p.Id).FirstOrDefaultAsync();

        if (provider == null)
            return Ok(new { success = false, message = "未找到视频生成提供者" });

        if (provider.Type == ProviderType.ComfyUI)
        {
            try
            {
                var proxyUrl = _configuration.GetValue<string>("ComfyuiProxyUrl") ?? "http://localhost:8288";
                var client = _httpClientFactory.CreateClient();
                var payload = new { prompt };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{proxyUrl.TrimEnd('/')}/api/comfyui/ltx/text-to-video", content);
                res.EnsureSuccessStatusCode();
                var body = await res.Content.ReadAsStringAsync();
                var response = System.Text.Json.JsonDocument.Parse(body);
                var promptId = response.RootElement.TryGetProperty("promptId", out var pid) ? pid.GetString() : null;
                if (string.IsNullOrEmpty(promptId))
                    return Ok(new { success = false, message = "ComfyUI 返回的 promptId 为空" });
                return Ok(new { success = true, promptId, workflowType = "ltx-text-to-video", isComfyui = true });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"ComfyUI 提交失败: {ex.Message}" });
            }
        }

        var result = await _aiAgent.GenerateVideoAsync(prompt, null, default, providerId);
        if (!result.success)
            return Ok(new { success = false, message = result.message });
        return Ok(new { success = true, data = result.resultUrl });
    }
}
