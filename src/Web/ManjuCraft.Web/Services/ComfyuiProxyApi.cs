using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Web.Services;

/// <summary>
/// ComfyUI Proxy 代理客户端 — ManjuCraft 通过此客户端调用 ComfyuiProxy 服务
/// </summary>
public class ComfyuiProxyApi
{
    private readonly HttpClient _http;

    public ComfyuiProxyApi(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient("ComfyuiProxy");
        _http.BaseAddress = new Uri(config["ComfyuiProxy:BaseUrl"] ?? "http://localhost:5212");
    }

    public async Task<string> SubmitAsync(string workflowType, string prompt, string? positivePrompt = null, string? imageUrl = null)
    {
        var req = new GenerateRequestDto
        {
            WorkflowType = workflowType,
            Prompt = prompt,
            PositivePrompt = positivePrompt,
            ImageUrl = imageUrl
        };

        var res = await _http.PostAsJsonAsync("/api/v1/comfyui/generate", req);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<GenerateResponseDto>();
        return data?.TaskId ?? "";
    }

    public async Task<GenerateResponseDto> GetTaskStatusAsync(string taskId)
    {
        var res = await _http.GetAsync($"/api/v1/comfyui/tasks/{taskId}");
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<GenerateResponseDto>();
    }

    public async Task<string> WaitForCompletionAsync(string taskId, int maxAttempts = 120, int intervalMs = 5000)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            var status = await GetTaskStatusAsync(taskId);
            if (status?.Status == "completed")
            {
                return status.Result?.Url ?? "";
            }
            if (status?.Status == "failed")
            {
                throw new Exception(status.Error ?? "Task failed");
            }
            await Task.Delay(intervalMs);
        }
        throw new TimeoutException("Proxy 任务超时");
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var res = await _http.GetAsync("/api/v1/comfyui/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => _http.Dispose();
}
