using ManjuCraft.Application.Service.ComfyuiProxy;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ManjuCraft.Application.AI;

public interface IAiAgentService
{
    Task<ApiProvider?> GetProviderAsync(long providerId, CancellationToken ct = default);
    Task<ApiProvider?> GetProviderByCapabilityAsync(AiCapability capability, CancellationToken ct = default);
    Task<(bool success, string? data, string? message, bool isComfyui, string? promptId, string? workflowType)> ChatAsync(long providerId, string systemPrompt, string userPrompt, CancellationToken ct = default);
    Task<(bool success, string? data, string? message, bool isComfyui, string? promptId, string? workflowType)> GenerateFrameImageAsync(string prompt, int width = 1024, int height = 576, long? providerId = null, CancellationToken ct = default);
    Task<(bool success, string? resultUrl, string? message)> GenerateImageAsync(string prompt, int? width = null, int? height = null, long? seed = null, long? providerId = null, CancellationToken ct = default);
    Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitCharacterProfileAsync(string systemPrompt, string characterPrompt, string? negativePrompt = null, int width = 1792, int height = 1024, CancellationToken ct = default);
    Task<(bool success, string? resultUrl, string? message)> GenerateVideoAsync(string prompt, string? imageUrl = null, CancellationToken ct = default, long? providerId = null);
    Task<(bool success, string? resultUrl, string? message)> GenerateAudioAsync(string prompt, string? tags = null, CancellationToken ct = default, long? providerId = null);
    Task<JsonElement> GetComfyuiResultAsync(string promptId, string workflowType, CancellationToken ct = default);
    Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitFrameImageWithAssetsAsync(string prompt, string compositeImagePath, long? providerId = null, CancellationToken ct = default);
    /// <summary>
    /// 使用 QWen 图生图工作流提交帧图片生成（角色/场景/道具分三张图传入）
    /// </summary>
    Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitFrameImageWithQwenEditAsync(string prompt, string? characterImagePath, string? sceneImagePath, string? propImagePath, long? providerId = null, CancellationToken ct = default);
    Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitShotVideoAsync(string prompt, string imagePath, long? providerId = null, CancellationToken ct = default);
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

    public async Task<(bool success, string? data, string? message, bool isComfyui, string? promptId, string? workflowType)> ChatAsync(long providerId, string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var provider = await GetProviderAsync(providerId, ct);
        if (provider == null)
            return (false, null, "未找到指定的 API 提供者", false, null, null);

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
                var submitBody = await res.Content.ReadFromJsonAsync<ComfyuiSubmitResponseDto>(cancellationToken: ct);
                var promptId = submitBody?.PromptId;
                if (string.IsNullOrEmpty(promptId))
                    return (false, null, "ComfyUI 返回的 promptId 为空", false, null, null);

                return (true, promptId, null, true, promptId, "llm-qwen-execute");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message, false, null, null);
            }
        }

        var client = _clientFactory.Create(provider);
        if (client == null)
            return (false, null, $"不支持的 API 类型: {provider.Name}", false, null, null);

        try
        {
            var result = await client.GenerateAsync(systemPrompt, userPrompt, ct);
            return (true, result, null, false, null, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message, false, null, null);
        }
    }

    public async Task<(bool success, string? data, string? message, bool isComfyui, string? promptId, string? workflowType)> GenerateFrameImageAsync(string prompt, int width = 1024, int height = 576, long? providerId = null, CancellationToken ct = default)
    {
        var provider = providerId.HasValue
            ? await GetProviderAsync(providerId!.Value, ct)
            : await GetProviderByCapabilityAsync(AiCapability.ImageToImage, ct);

        if (provider == null)
            return (false, null, "未找到图像生成提供者", false, null, null);

        // ComfyUI → 通过 HiDream 分镜工作流生成图片，返回 promptId 供前端轮询
        if (provider.Type == Domain.Models.ProviderType.ComfyUI)
        {
            try
            {
                var payload = new { prompt, imagePath = (string?)null };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var res = await _http.PostAsync($"{_comfyuiProxyUrl.TrimEnd('/')}/api/comfyui/hidream/storyboard", content, ct);
                res.EnsureSuccessStatusCode();
                var body = await res.Content.ReadFromJsonAsync<ComfyuiSubmitResponseDto>(cancellationToken: ct);
                var promptId = body?.PromptId;
                if (string.IsNullOrEmpty(promptId))
                    return (false, null, "HiDream 返回的 promptId 为空", false, null, null);
                return (true, promptId, null, true, promptId, "hidream-storyboard");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message, false, null, null);
            }
        }

        // 标准 API → 直接生成并返回图片 URL
        try
        {
            var apiBody = new { model = provider.Model, prompt, n = 1, size = $"{width}x{height}" };
            var apiJson = JsonSerializer.Serialize(apiBody);
            var apiContent = new StringContent(apiJson, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(provider.ApiKey))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);

            var apiRes = await _http.PostAsync($"{provider.ApiUrl.TrimEnd('/')}/images/generations", apiContent, ct);
            var apiText = await apiRes.Content.ReadAsStringAsync(ct);
            if (!apiRes.IsSuccessStatusCode)
                return (false, null, $"API 错误: {apiText}", false, null, null);

            var j = JsonDocument.Parse(apiText);
            var url = j.RootElement.GetProperty("data")[0].GetProperty("url").GetString();
            return (true, url, null, false, null, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message, false, null, null);
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

    public async Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitCharacterProfileAsync(string systemPrompt, string characterPrompt, string? negativePrompt = null, int width = 1792, int height = 1024, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                systemPrompt,
                characterPrompt,
                negativePrompt = negativePrompt ?? string.Empty,
                width,
                height
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{_comfyuiProxyUrl.TrimEnd('/')}/api/comfyui/zimage/character-profile", content, ct);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<ComfyuiSubmitResponseDto>(cancellationToken: ct);
            var promptId = body?.PromptId;
            if (string.IsNullOrEmpty(promptId))
                return (false, null, null, "ComfyUI 返回的 promptId 为空");
            return (true, promptId, "zimage-character-profile", null);
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
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

    /// <summary>
    /// 上传本地图片到 ComfyUI 输入目录，返回 ComfyUI 中的文件名
    /// </summary>
    private async Task<string?> UploadToComfyuiAsync(string? imagePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath))
            return null;

        using var formData = new MultipartFormDataContent();
        var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        var streamContent = new StreamContent(fileStream);
        formData.Add(streamContent, "image", Path.GetFileName(imagePath));
        formData.Add(new StringContent(""), "subfolder");

        var uploadRes = await _http.PostAsync($"{_comfyuiProxyUrl.TrimEnd('/')}/api/comfyui/upload", formData, ct);
        fileStream.Close();

        if (!uploadRes.IsSuccessStatusCode)
            return null;

        var uploadBody = await uploadRes.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return uploadBody.TryGetProperty("name", out var fn) ? fn.GetString()
            : uploadBody.TryGetProperty("filename", out var fn2) ? fn2.GetString() : null;
    }

    /// <summary>
    /// 提交帧图片生成任务（HiDream 分镜工作流），将所有资产合并为一张图后上传到 ComfyUI
    /// </summary>
    public async Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitFrameImageWithAssetsAsync(string prompt, string compositeImagePath, long? providerId = null, CancellationToken ct = default)
    {
        var provider = providerId.HasValue
            ? await GetProviderAsync(providerId!.Value, ct)
            : await GetProviderByCapabilityAsync(AiCapability.ImageToImage, ct);

        if (provider == null || provider.Type != ProviderType.ComfyUI)
            return (false, null, null, "未找到可用的 ComfyUI ImageToImage 提供者");

        try
        {
            var compositeImageFilename = await UploadToComfyuiAsync(compositeImagePath, ct);

            var payload = new
            {
                prompt,
                imagePath = compositeImageFilename ?? ""
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{_comfyuiProxyUrl.TrimEnd('/')}/api/comfyui/hidream/storyboard", content, ct);
            res.EnsureSuccessStatusCode();
            var responseBody = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            var promptId = responseBody.TryGetProperty("promptId", out var pid) ? pid.GetString() : null;
            if (string.IsNullOrEmpty(promptId))
                return (false, null, null, "ComfyUI 返回的 promptId 为空");

            return (true, promptId, "hidream-storyboard", null);
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
    }

    /// <summary>
    /// 提交帧图片生成任务（QWen 图生图工作流），将角色/场景/道具分别上传到 ComfyUI 的三个图片位
    /// </summary>
    /// <param name="prompt">生成提示词</param>
    /// <param name="characterImagePath">角色合成图片路径（图1，可为 null）</param>
    /// <param name="sceneImagePath">场景合成图片路径（图2，可为 null）</param>
    /// <param name="propImagePath">道具合成图片路径（图3，可为 null）</param>
    /// <param name="providerId">AI 提供者 ID（null 则按 ImageToImageQwen 能力自动查找）</param>
    public async Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitFrameImageWithQwenEditAsync(string prompt, string? characterImagePath, string? sceneImagePath, string? propImagePath, long? providerId = null, CancellationToken ct = default)
    {
        var provider = providerId.HasValue
            ? await GetProviderAsync(providerId!.Value, ct)
            : await GetProviderByCapabilityAsync(AiCapability.ImageToImageQwen, ct);

        if (provider == null || provider.Type != ProviderType.ComfyUI)
            return (false, null, null, "未找到可用的 ComfyUI ImageToImageQwen 提供者");

        try
        {
            var uploaded1 = await UploadToComfyuiAsync(characterImagePath, ct);
            var uploaded2 = await UploadToComfyuiAsync(sceneImagePath, ct);
            var uploaded3 = await UploadToComfyuiAsync(propImagePath, ct);

            var payload = new
            {
                imagePath1 = uploaded1 ?? "",
                imagePath2 = uploaded2 ?? "",
                imagePath3 = uploaded3 ?? "",
                prompt,
                width = 1920,
                height = 1080,
                enableLightningLora = true,
                seed = -1
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{_comfyuiProxyUrl.TrimEnd('/')}/api/comfyui/llm-qwen/image-edit", content, ct);
            res.EnsureSuccessStatusCode();
            var responseBody = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            var promptId = responseBody.TryGetProperty("promptId", out var pid) ? pid.GetString() : null;
            if (string.IsNullOrEmpty(promptId))
                return (false, null, null, "ComfyUI 返回的 promptId 为空");

            return (true, promptId, "qwen-image-edit", null);
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
    }

    public async Task<(bool success, string? promptId, string? workflowType, string? message)> SubmitShotVideoAsync(string prompt, string imagePath, long? providerId = null, CancellationToken ct = default)
    {
        var provider = providerId.HasValue
            ? await GetProviderAsync(providerId!.Value, ct)
            : await GetProviderByCapabilityAsync(AiCapability.ImageToVideo, ct);

        if (provider == null)
            return (false, null, null, "未找到视频生成提供者（需 Capability=ImageToVideo）");

        if (provider.Type == ProviderType.ComfyUI)
        {
            try
            {
                var uploadedFilename = await UploadToComfyuiAsync(imagePath, ct);

                var payload = new
                {
                    prompt,
                    imagePath = uploadedFilename ?? "",
                    width = 1280,
                    height = 720,
                    duration = 5,
                    fps = 24,
                    seed = -1
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var res = await _http.PostAsync($"{_comfyuiProxyUrl.TrimEnd('/')}/api/comfyui/ltx/image-to-video", content, ct);
                res.EnsureSuccessStatusCode();
                var responseBody = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

                var promptId = responseBody.TryGetProperty("promptId", out var pid) ? pid.GetString() : null;
                if (string.IsNullOrEmpty(promptId))
                    return (false, null, null, "ComfyUI 返回的 promptId 为空");

                return (true, promptId, "ltx-image-to-video", null);
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        var result = await GenerateVideoAsync(prompt, imagePath, ct, providerId);
        if (!result.success)
            return (false, null, null, result.message);
        return (true, result.resultUrl, null, null);
    }

    public async Task<JsonElement> GetComfyuiResultAsync(string promptId, string workflowType, CancellationToken ct = default)
    {
        try
        {
            var baseUrl = _comfyuiProxyUrl.TrimEnd('/');
            var url = $"{baseUrl}/api/comfyui/result/{promptId}?workflowType={workflowType}";
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
                return new JsonElement();

            var body = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return body;
        }
        catch
        {
            return new JsonElement();
        }
    }
}
