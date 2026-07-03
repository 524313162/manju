using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ManjuCraft.Application.Service;

namespace ManjuCraft.Application.LLM.Providers;

public class DeepSeekClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly ILogger<DeepSeekClient> _logger;
    private readonly string _apiKey, _apiUrl, _model;

    public DeepSeekClient(IHttpClientFactory httpFactory, IOptionsSnapshot<DeepSeekOptions> opts, ILogger<DeepSeekClient> logger)
    {
        _http = httpFactory.CreateClient();
        _logger = logger;
        _apiKey = opts.Value.ApiKey;
        _apiUrl = opts.Value.ApiUrl;
        _model = opts.Value.Model;
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey)) { _logger.LogWarning("DeepSeek API Key 未配置"); return ""; }
        var body = new { model = _model, messages = new[] {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userContent }
        }, stream = false };
        var req = new HttpRequestMessage(HttpMethod.Post, _apiUrl.TrimEnd('/') + "/chat/completions");
        req.Content = JsonContent.Create(body);
        req.Headers.Authorization = new("Bearer", _apiKey);
        try
        {
            var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<DeepSeekResponse>();
            return data?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }
        catch (Exception ex) { _logger.LogError(ex, "DeepSeek 调用失败"); return ""; }
    }

    class DeepSeekResponse { public List<Choice>? Choices { get; set; } }
    class Choice { public Message? Message { get; set; } }
    class Message { public string? Content { get; set; } }
}