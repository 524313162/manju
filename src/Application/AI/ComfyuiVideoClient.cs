using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

/// <summary>
/// ComfyUI 视频客户端 — 通过 ComfyUI Proxy 调用工作流
/// </summary>
public class ComfyuiVideoClient : ITextToVideoClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ComfyuiVideoClient> _logger;
    private readonly string _proxyUrl;
    private readonly string _workflowName;

    public ComfyuiVideoClient(string apiUrl, string apiKey, string model, ILogger<ComfyuiVideoClient> logger)
    {
        _logger = logger;
        _proxyUrl = apiUrl?.TrimEnd('/') ?? "";
        _workflowName = model ?? "LLM-QWEN.json";
        _http = new HttpClient { BaseAddress = new Uri(_proxyUrl), Timeout = TimeSpan.FromMinutes(5) };
        Capability = AiCapability.TextToVideo;
    }

    public AiCapability Capability { get; set; }

    public async Task<string> GenerateAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        var body = new
        {
            workflow_type = _workflowName,
            prompt,
            reference_images = referenceImages ?? new List<string>()
        };

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/comfyui/generate");
            req.Content = JsonContent.Create(body);
            req.Headers.Authorization = new("Bearer", "");

            var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<ComfyuiResponse>(cancellationToken: ct);

            var taskId = data?.TaskId;
            if (string.IsNullOrEmpty(taskId))
            {
                return data?.Status ?? "Task submitted";
            }

            // Poll for completion
            var maxAttempts = 60;
            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                var statusRes = await _http.GetAsync($"/api/v1/comfyui/tasks/{taskId}", ct);
                var statusData = await statusRes.Content.ReadFromJsonAsync<ComfyuiResponse>(cancellationToken: ct);

                if (statusData?.Status == "completed")
                {
                    _logger.LogInformation("ComfyUI task { taskId} completed: {Url}", taskId, statusData.Result?.Url);
                    return statusData.Result?.Url ?? $"Task {taskId} completed";
                }
                if (statusData?.Status == "failed")
                {
                    _logger.LogError("ComfyUI task {0} failed: {1}", taskId, statusData.Error);
                    return $"Task failed: {statusData.Error}";
                }
            }

            return $"Task {taskId} still pending after {maxAttempts} attempts";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ComfyUI 调用失败");
            return $"Error: {ex.Message}";
        }
    }

    private class ComfyuiResponse
    {
        public string? TaskId { get; set; }
        public string? Status { get; set; }
        public ComfyuiResult? Result { get; set; }
        public string? Error { get; set; }
    }

    private class ComfyuiResult
    {
        public string? Url { get; set; }
    }
}
