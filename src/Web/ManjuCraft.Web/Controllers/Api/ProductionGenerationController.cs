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
    public async Task<IActionResult> GenerateVideo([FromBody] GenerateShotVideoRequestDto req)
    {
        if (string.IsNullOrEmpty(req.Prompt))
            return Ok(new { success = false, message = "提示词不能为空" });

        if (!string.IsNullOrEmpty(req.ImagePath))
        {
            var wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var localPath = Path.Combine(wwwRoot, req.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(localPath))
                return Ok(new { success = false, message = $"图片文件不存在: {req.ImagePath}" });

            var result = await _aiAgent.SubmitShotVideoAsync(req.Prompt, localPath, req.ProviderId > 0 ? req.ProviderId : null);
            if (!result.success)
                return Ok(new { success = false, message = result.message });
            if (result.workflowType != null)
                return Ok(new { success = true, promptId = result.promptId, workflowType = result.workflowType, isComfyui = true });
            return Ok(new { success = true, data = result.promptId });
        }

        var provider = req.ProviderId.HasValue
            ? await _db.ApiProviders.FirstOrDefaultAsync(p => p.Id == req.ProviderId.Value)
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
                var payload = new { prompt = req.Prompt };
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

        var genResult = await _aiAgent.GenerateVideoAsync(req.Prompt, null, default, req.ProviderId);
        if (!genResult.success)
            return Ok(new { success = false, message = genResult.message });
        return Ok(new { success = true, data = genResult.resultUrl });
    }
}

public class GenerateShotVideoRequestDto
{
    public string Prompt { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public long? ProviderId { get; set; }
}
