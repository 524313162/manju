using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

/// <summary>
/// ComfyUI 客户端 — 通过 ApiProvider 配置的代理地址调用工作流
/// Model 字段存储工作流名称（如 LLM-QWEN.json），ApiUrl 存储代理程序地址
/// </summary>
public class ComfyuiVideoClient : ITextToVideoClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ComfyuiVideoClient> _logger;
    private readonly string _workflowName;

    public ComfyuiVideoClient(string apiUrl, string apiKey, string model, ILogger<ComfyuiVideoClient> logger)
    {
        _logger = logger;
        _workflowName = model ?? "LLM-QWEN.json";
        var url = (apiUrl ?? "http://localhost:8288").TrimEnd('/');
        _http = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromMinutes(5) };
        Capability = AiCapability.TextToVideo;

        _logger.LogInformation("ComfyUI 客户端初始化: ApiUrl={Url}, Workflow={Workflow}", url, _workflowName);
    }

    public AiCapability Capability { get; set; }

    public async Task<string> GenerateAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        _logger.LogInformation("ComfyUI 调用工作流 {Workflow}: {Prompt}", _workflowName, prompt);

        try
        {
            var res = await _http.PostAsJsonAsync("/api/v1/comfyui/generate", new
            {
                workflow_type = _workflowName,
                prompt,
                reference_images = referenceImages ?? new List<string>()
            }, ct);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<ProxyResponse>(cancellationToken: ct);

            var taskId = data?.TaskId;
            if (string.IsNullOrEmpty(taskId))
                return data?.Status ?? "Task submitted";

            var maxAttempts = 60;
            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                var statusRes = await _http.GetAsync($"/api/v1/comfyui/tasks/{taskId}", ct);
                var statusData = await statusRes.Content.ReadFromJsonAsync<ProxyResponse>(cancellationToken: ct);

                if (statusData?.Status == "completed")
                {
                    _logger.LogInformation("ComfyUI task {TaskId} completed: {Url}", taskId, statusData.Result?.Url);
                    return statusData.Result?.Url ?? $"Task {taskId} completed";
                }
                if (statusData?.Status == "failed")
                {
                    _logger.LogError("ComfyUI task {TaskId} failed: {Error}", taskId, statusData.Error);
                    return $"Task failed: {statusData.Error}";
                }
            }
            return $"Task {taskId} still pending";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ComfyUI {Workflow} 调用失败", _workflowName);
            return $"Error: {ex.Message}";
        }
    }

    private class ProxyResponse
    {
        public string? TaskId { get; set; }
        public string? Status { get; set; }
        public ProxyResult? Result { get; set; }
        public string? Error { get; set; }
    }
    private class ProxyResult { public string? Url { get; set; } }
}
