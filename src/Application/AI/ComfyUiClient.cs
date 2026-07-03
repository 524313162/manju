using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

/// <summary>
/// ComfyUI AI 客户端 — 实现多种图像/视频能力
/// 具体能力由 Capability 属性决定
/// </summary>
public class ComfyUiClient : ITextToImageClient, ITextToVideoClient, IImageToVideoClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ComfyUiClient> _logger;

    public ComfyUiClient(string apiUrl, ILogger<ComfyUiClient> logger)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiUrl.TrimEnd('/')) };
        _http.Timeout = TimeSpan.FromMinutes(10);
        _logger = logger;
    }

    public AiCapability Capability { get; set; }

    // ITextToImageClient
    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("ComfyUI 文生图: {Prompt}", prompt);
        return await ExecuteAsync(new { prompt }, ct);
    }

    // ITextToVideoClient
    public async Task<string> GenerateAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        _logger.LogInformation("ComfyUI 文生视频: {Prompt}", prompt);
        return await ExecuteAsync(new { prompt, referenceImages }, ct);
    }

    // IImageToVideoClient
    public async Task<string> GenerateAsync(string prompt, string referenceImagePath, CancellationToken ct = default)
    {
        _logger.LogInformation("ComfyUI 图生视频: {Prompt}", prompt);
        return await ExecuteAsync(new { prompt, referenceImagePath }, ct);
    }

    private async Task<string> ExecuteAsync(object data, CancellationToken ct)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/prompt");
            req.Content = JsonContent.Create(data);

            var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                var error = await res.Content.ReadAsStringAsync(ct);
                throw new Exception($"ComfyUI 提交失败 ({res.StatusCode}): {error}");
            }

            var result = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ct);
            if (result == null)
                return "";

            var promptId = result.ContainsKey("prompt_id") ? result["prompt_id"].ToString()! : "";
            return await PollForResultAsync(promptId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ComfyUI 调用失败");
            return "";
        }
    }

    private async Task<string> PollForResultAsync(string promptId, CancellationToken ct)
    {
        await Task.Delay(5000, ct);
        return "/mock/output/result.png";
    }
}
