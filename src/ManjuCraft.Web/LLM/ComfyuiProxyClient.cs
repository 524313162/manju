using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ManjuCraft.Web.LLM;

public class ComfyuiProxyClient : IComfyuiProxyClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ComfyuiProxyClient> _logger;
    private readonly string _proxyUrl;

    public ComfyuiProxyClient(IHttpClientFactory httpFactory, IConfiguration config, ILogger<ComfyuiProxyClient> logger)
    {
        _http = httpFactory.CreateClient();
        _logger = logger;
        _proxyUrl = config.GetValue<string>("ComfyuiProxy:ApiUrl") ?? "http://localhost:8288";
    }

    public async Task<string> GenerateImageAsync(string workflowType, string prompt, string? positivePrompt = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[ComfyuiProxy] {Type} 生图: {Prompt}", workflowType, prompt);
        var payload = new { workflow_type = workflowType, prompt, positive_prompt = positivePrompt };
        var res = await _http.PostAsJsonAsync($"{_proxyUrl.TrimEnd('/')}/api/generate", payload, ct);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<ProxyResponse>(cancellationToken: ct);
        return data?.Result?.Url ?? $"/mock/proxy/{workflowType}_{Guid.NewGuid():N}.png";
    }

    public async Task<string> GenerateVideoAsync(string workflowType, string prompt, string? imageUrl = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[ComfyuiProxy] {Type} 生视频: {Prompt}", workflowType, prompt);
        var payload = new { workflow_type = workflowType, prompt, image_url = imageUrl };
        var res = await _http.PostAsJsonAsync($"{_proxyUrl.TrimEnd('/')}/api/generate", payload, ct);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<ProxyResponse>(cancellationToken: ct);
        return data?.Result?.Url ?? $"/mock/proxy/{workflowType}_{Guid.NewGuid():N}.mp4";
    }

    public async Task<string> GenerateAudioAsync(string workflowType, string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("[ComfyuiProxy] {Type} 生音频: {Prompt}", workflowType, prompt);
        var payload = new { workflow_type = workflowType, prompt };
        var res = await _http.PostAsJsonAsync($"{_proxyUrl.TrimEnd('/')}/api/generate", payload, ct);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<ProxyResponse>(cancellationToken: ct);
        return data?.Result?.Url ?? $"/mock/proxy/{workflowType}_{Guid.NewGuid():N}.mp3";
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"{_proxyUrl.TrimEnd('/')}/health", ct);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    class ProxyResponse { public string? TaskId { get; set; } public string? Status { get; set; } public ProxyResult? Result { get; set; } }
    class ProxyResult { public string? Url { get; set; } }
}

public interface IComfyuiProxyClient
{
    Task<string> GenerateImageAsync(string workflowType, string prompt, string? positivePrompt = null, CancellationToken ct = default);
    Task<string> GenerateVideoAsync(string workflowType, string prompt, string? imageUrl = null, CancellationToken ct = default);
    Task<string> GenerateAudioAsync(string workflowType, string prompt, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}