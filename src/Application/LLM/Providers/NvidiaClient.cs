using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ManjuCraft.Application.LLM.Providers;

public class NvidiaClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NvidiaClient> _logger;
    private readonly string _apiKey, _apiUrl, _model;

    public NvidiaClient(IHttpClientFactory httpFactory, IConfiguration config, ILogger<NvidiaClient> logger)
    {
        _http = httpFactory.CreateClient();
        _logger = logger;
        var section = config.GetSection("Ai:Providers:NVIDIA");
        _apiKey = section["ApiKey"] ?? "";
        _apiUrl = section["ApiUrl"] ?? "https://integrate.api.nvidia.com/v1";
        _model = section["Model"] ?? "nvidia/llama-3.1-nemotron-70b-instruct";
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey)) { _logger.LogWarning("NVIDIA API Key 未配置"); return ""; }
        var body = new { model = _model, messages = new[] {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userContent }
        } };
        var req = new HttpRequestMessage(HttpMethod.Post, _apiUrl.TrimEnd('/') + "/chat/completions");
        req.Content = JsonContent.Create(body);
        req.Headers.Authorization = new("Bearer", _apiKey);
        try
        {
            var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<Response>();
            return data?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }
        catch (Exception ex) { _logger.LogError(ex, "NVIDIA 调用失败"); return ""; }
    }

    class Response { public List<Choice>? Choices { get; set; } }
    class Choice { public Msg? Message { get; set; } }
    class Msg { public string? Content { get; set; }
    }
}