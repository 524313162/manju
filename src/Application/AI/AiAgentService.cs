using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ManjuCraft.Application.AI;

public interface IAiAgentService
{
    Task<ApiProvider?> GetProviderAsync(long providerId, CancellationToken ct = default);
    Task<ApiProvider?> GetProviderByCapabilityAsync(AiCapability capability, CancellationToken ct = default);
    Task<(bool success, string? data, string? message)> ChatAsync(long providerId, string systemPrompt, string userPrompt, CancellationToken ct = default);
    Task<(bool success, string? resultUrl, string? message)> GenerateImageAsync(string prompt, int? width = null, int? height = null, long? seed = null, long? providerId = null, CancellationToken ct = default);
    Task<(bool success, string? resultUrl, string? message)> GenerateVideoAsync(string prompt, string? imageUrl = null, CancellationToken ct = default, long? providerId = null);
    Task<(bool success, string? resultUrl, string? message)> GenerateAudioAsync(string prompt, string? tags = null, CancellationToken ct = default, long? providerId = null);
}

public class AiAgentService : IAiAgentService
{
    private readonly IProjectDbContext _db;
    private readonly IAiChatClientFactory _clientFactory;
    private readonly HttpClient _http;
    private readonly string _comfyuiProxyUrl;

    public AiAgentService(IProjectDbContext db, IAiChatClientFactory clientFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _db = db;
        _clientFactory = clientFactory;
        _http = httpClientFactory.CreateClient("ai_agent");
        _comfyuiProxyUrl = configuration.GetValue<string>("ComfyuiProxy:ApiUrl") ?? "http://localhost:8288";
    }

    public Task<ApiProvider?> GetProviderAsync(long providerId, CancellationToken ct = default)
    {
        return _db.ApiProviders.FirstOrDefaultAsync(p => p.Id == providerId, ct);
    }

    public Task<ApiProvider?> GetProviderByCapabilityAsync(AiCapability capability, CancellationToken ct = default)
    {
        return _db.ApiProviders.FirstOrDefaultAsync(p => p.Capability == capability, ct);
    }

    public async Task<(bool success, string? data, string? message)> ChatAsync(long providerId, string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var provider = await GetProviderAsync(providerId, ct);
        if (provider == null)
            return (false, null, "未找到指定的 API 提供者");

        // ComfyUI(Proxy) — 走 ComfyUI 代理 LLM
        if (provider.Type == Domain.Models.ProviderType.ComfyUI)
        {
            try
            {
                var combinedPrompt = string.IsNullOrEmpty(systemPrompt)
                    ? userPrompt
                    : $"{systemPrompt}\n\n{userPrompt}";
                var payload = new { prompt = combinedPrompt };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var res = await _http.PostAsync($"{_comfyuiProxyUrl.TrimEnd('/')}/api/comfyui/llm-qwen/execute", content, ct);
                res.EnsureSuccessStatusCode();
                var text = await res.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<LlmProxyResponse>(text);
                return (true, result?.Text, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        var client = _clientFactory.Create(provider);
        if (client == null)
            return (false, null, $"不支持的 API 类型: {provider.Name}");

        try
        {
            var result = await client.GenerateAsync(systemPrompt, userPrompt, ct);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, string? resultUrl, string? message)> GenerateImageAsync(string prompt, int? width = null, int? height = null, long? seed = null, long? providerId = null, CancellationToken ct = default)
    {
        var provider = providerId.HasValue
            ? await GetProviderAsync(providerId!.Value)
            : await GetProviderByCapabilityAsync(AiCapability.TextToImage);

        if (provider == null)
            return (false, null, "未找到图像生成提供者（需 Capability=TextToImage 或 ComfyUI）");

        if (provider.Type == Domain.Models.ProviderType.ComfyUI)
        {
            return await GenerateImageViaComfyui(proxyUrl: "http://localhost:8288", prompt, width, height, seed);
        }

        // 第三方 API — 走 OpenAI 兼容接口
        return await GenerateImageViaApi(provider, prompt, width, height, seed, ct);
    }

    public async Task<(bool success, string? resultUrl, string? message)> GenerateVideoAsync(string prompt, string? imageUrl = null, CancellationToken ct = default, long? providerId = null)
    {
        var provider = providerId.HasValue
            ? await GetProviderAsync(providerId!.Value)
            : await GetProviderByCapabilityAsync(AiCapability.TextToVideo);

        if (provider == null)
            return (false, null, "未找到视频生成提供者（需 Capability=TextToVideo 或 ComfyUI）");

        if (provider.Type == Domain.Models.ProviderType.ComfyUI)
        {
            return await GenerateVideoViaComfyui(proxyUrl: "http://localhost:8288", prompt, imageUrl);
        }

        if (provider.Name?.ToLowerInvariant().Contains("kling") == true)
        {
            return await GenerateVideoViaKling(provider, prompt, ct);
        }

        return await GenerateVideoViaApi(provider, prompt, imageUrl, ct);
    }

    public async Task<(bool success, string? resultUrl, string? message)> GenerateAudioAsync(string prompt, string? tags = null, CancellationToken ct = default, long? providerId = null)
    {
        var provider = providerId.HasValue
            ? await GetProviderAsync(providerId!.Value, ct)
            : await GetProviderByCapabilityAsync(AiCapability.TextToAudio);

        if (provider == null)
        {
            provider = await GetProviderByCapabilityAsync(AiCapability.TextToMusic);
        }

        if (provider == null)
            return (false, null, "未找到音频生成提供者（需 Capability=TextToAudio/TextToMusic 或 ComfyUI）");

        if (provider.Type == Domain.Models.ProviderType.ComfyUI)
        {
            return await GenerateAudioViaComfyui(proxyUrl: "http://localhost:8288", prompt ?? tags ?? "");
        }

        if (provider.Name?.ToLowerInvariant().Contains("suno") == true)
        {
            return await GenerateAudioViaSuno(provider, prompt, tags, ct);
        }

        return await GenerateAudioViaApi(provider, prompt, ct);
    }

    // ── ComfyUI 本地代理 ──

    private async Task<(bool, string?, string?)> GenerateImageViaComfyui(string proxyUrl, string prompt, int? width, int? height, long? seed)
    {
        try
        {
            var payload = new { workflow_type = "zimage_text_to_image", prompt, positive_prompt = prompt,
                width = width ?? 1024, height = height ?? 1024, seed = seed ?? 0 };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{proxyUrl.TrimEnd('/')}/api/v1/comfyui/generate", content);
            res.EnsureSuccessStatusCode();
            var text = await res.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ComfyProxyResponse>(text);
            return (true, data?.Result?.Url ?? string.Empty, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"ComfyUI 生图失败: {ex.Message}");
        }
    }

    private async Task<(bool, string?, string?)> GenerateVideoViaComfyui(string proxyUrl, string prompt, string? imageUrl)
    {
        try
        {
            var payload = new { workflow_type = "ltx_text_to_video", prompt, image_url = imageUrl };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{proxyUrl.TrimEnd('/')}/api/v1/comfyui/generate", content);
            res.EnsureSuccessStatusCode();
            var text = await res.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ComfyProxyResponse>(text);
            return (true, data?.Result?.Url ?? string.Empty, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"ComfyUI 生成视频失败: {ex.Message}");
        }
    }

    private async Task<(bool, string?, string?)> GenerateAudioViaComfyui(string proxyUrl, string prompt)
    {
        try
        {
            var payload = new { workflow_type = "ace_music", prompt };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{proxyUrl.TrimEnd('/')}/api/v1/comfyui/generate", content);
            res.EnsureSuccessStatusCode();
            var text = await res.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ComfyProxyResponse>(text);
            return (true, data?.Result?.Url ?? string.Empty, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"ComfyUI 生成音频失败: {ex.Message}");
        }
    }

    // ── 第三方 API 生成 ──

    private async Task<(bool, string?, string?)> GenerateImageViaApi(ApiProvider provider, string prompt, int? width, int? height, long? seed, CancellationToken ct)
    {
        try
        {
            var body = new
            {
                model = provider.Model,
                prompt,
                n = 1,
                size = $"{width ?? 1024}x{height ?? 1024}"
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(provider.ApiKey))
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);

            var res = await _http.PostAsync($"{provider.ApiUrl.TrimEnd('/')}/images/generations", content, ct);
            var text = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                return (false, null, $"API 错误: {text}");

            var j = System.Text.Json.JsonDocument.Parse(text);
            var url = j.RootElement.GetProperty("data")[0].GetProperty("url").GetString();
            return (true, url, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool, string?, string?)> GenerateVideoViaApi(ApiProvider provider, string prompt, string? imageUrl, CancellationToken ct)
    {
        try
        {
            var body = new { model = provider.Model, prompt, image_url = imageUrl, n = 1 };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(provider.ApiKey))
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);

            var res = await _http.PostAsync($"{provider.ApiUrl.TrimEnd('/')}/videos", content, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                return (false, null, $"API 错误: {text}");

            var j = System.Text.Json.JsonDocument.Parse(text);
            var url = j.RootElement.GetProperty("data")[0].GetProperty("url").GetString();
            return (true, url, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool, string?, string?)> GenerateVideoViaKling(ApiProvider provider, string prompt, CancellationToken ct)
    {
        try
        {
            var body = new
            {
                model = provider.Model ?? "kling-v1-6",
                prompt,
                negative_prompt = "",
                duration_seconds = 5,
                camera_control = "free"
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {provider.ApiKey}");

            var res = await _http.PostAsync($"{provider.ApiUrl.TrimEnd('/')}/v1/images/videos", content, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                return (false, null, $"Kling API 错误: {text}");

            var j = System.Text.Json.JsonDocument.Parse(text);
            var taskId = j.RootElement.GetProperty("data").GetProperty("task_id").GetString();
            return (true, taskId, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool, string?, string?)> GenerateAudioViaSuno(ApiProvider provider, string prompt, string? tags, CancellationToken ct)
    {
        try
        {
            var body = new
            {
                prompt = prompt,
                tags = tags ?? "",
                make_instrumental = false,
                model = provider.Model ?? "suno-v3.5"
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {provider.ApiKey}");

            var res = await _http.PostAsync($"{provider.ApiUrl.TrimEnd('/')}/v1/pages", content, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                return (false, null, $"Suno API 错误: {text}");

            var j = System.Text.Json.JsonDocument.Parse(text);
            var tracks = j.RootElement.GetProperty("data").GetProperty("tracks");
            var url = tracks[0].GetProperty("audio_url").GetString();
            return (true, url, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool, string?, string?)> GenerateAudioViaApi(ApiProvider provider, string prompt, CancellationToken ct)
    {
        try
        {
            var body = new { model = provider.Model, prompt };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(provider.ApiKey))
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);

            var res = await _http.PostAsync($"{provider.ApiUrl.TrimEnd('/')}/audio", content, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                return (false, null, $"API 错误: {text}");

            var j = System.Text.Json.JsonDocument.Parse(text);
            var url = j.RootElement.GetProperty("data").GetProperty("url").GetString();
            return (true, url, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private class ComfyProxyResponse
    {
        public string? TaskId { get; set; }
        public string? Status { get; set; }
        public ComfyProxyResult? Result { get; set; }
    }

    private class ComfyProxyResult
    {
        public string? Url { get; set; }
    }

    private class LlmProxyResponse
    {
        public string? Text { get; set; }
    }
}
