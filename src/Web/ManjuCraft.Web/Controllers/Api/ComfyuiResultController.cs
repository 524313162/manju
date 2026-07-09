using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.AI;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ManjuCraft.Web.Controllers.Api;

[ApiController]
[Route("api/v1/comfyui")]
public class ComfyuiResultController : ControllerBase
{
    private readonly IAiAgentService _aiAgent;

    public ComfyuiResultController(IAiAgentService aiAgent)
    {
        _aiAgent = aiAgent;
    }

    private static bool IsEmptyObject(JsonElement el)
    {
        return el.ValueKind == JsonValueKind.Object && !el.EnumerateObject().Any();
    }

    [HttpGet("result/{promptId}/text")]
    public async Task<IActionResult> GetTextResult(string promptId, [FromQuery] string workflowType)
    {
        var body = await _aiAgent.GetComfyuiResultAsync(promptId, workflowType);
        if (IsEmptyObject(body))
            return Ok(new { success = false, pending = true });

        var text = body.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        if (string.IsNullOrEmpty(text))
            return Ok(new { success = false, message = "生成结果为空" });

        var cleaned = CleanLlmText(text);
        return Ok(new { success = true, data = new { text = cleaned } });
    }

    private static string CleanLlmText(string raw)
    {
        raw = Regex.Replace(raw, @"<think>[\s\S]*?</think>", "").Trim();

        // 提取 ```json ... ``` 代码块
        var jsonMatch = Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)```");
        var jsonText = jsonMatch.Success ? jsonMatch.Groups[1].Value.Trim() : raw;

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            // { "content": "..." } → 提取 content
            if (root.TryGetProperty("content", out var ct) && ct.ValueKind == JsonValueKind.String)
                return ct.GetString() ?? raw;

            // { "chapters": [...] } → 返回完整 JSON
            return jsonText;
        }
        catch
        {
            return raw;
        }
    }

    [HttpGet("result/{promptId}/image")]
    public async Task<IActionResult> GetImageResult(string promptId, [FromQuery] string workflowType)
    {
        var body = await _aiAgent.GetComfyuiResultAsync(promptId, workflowType);
        if (IsEmptyObject(body))
            return Ok(new { success = false, pending = true });

        var urls = body.TryGetProperty("imageUrls", out var u) && u.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<string>>(u.GetRawText()) ?? new List<string>()
            : new List<string>();
        if (urls.Count == 0)
            return Ok(new { success = false, message = "生成结果为空" });
        return Ok(new { success = true, data = new { imageUrls = urls } });
    }

    [HttpGet("result/{promptId}/video")]
    public async Task<IActionResult> GetVideoResult(string promptId, [FromQuery] string workflowType)
    {
        var body = await _aiAgent.GetComfyuiResultAsync(promptId, workflowType);
        if (IsEmptyObject(body))
            return Ok(new { success = false, pending = true });

        var urls = body.TryGetProperty("videoUrls", out var u) && u.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<string>>(u.GetRawText()) ?? new List<string>()
            : new List<string>();
        if (urls.Count == 0)
            return Ok(new { success = false, message = "生成结果为空" });
        return Ok(new { success = true, data = new { videoUrls = urls } });
    }

    [HttpGet("result/{promptId}/audio")]
    public async Task<IActionResult> GetAudioResult(string promptId, [FromQuery] string workflowType)
    {
        var body = await _aiAgent.GetComfyuiResultAsync(promptId, workflowType);
        if (IsEmptyObject(body))
            return Ok(new { success = false, pending = true });

        var urls = body.TryGetProperty("audioUrls", out var u) && u.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<string>>(u.GetRawText()) ?? new List<string>()
            : new List<string>();
        if (urls.Count == 0)
            return Ok(new { success = false, message = "生成结果为空" });
        return Ok(new { success = true, data = new { audioUrls = urls } });
    }
}
