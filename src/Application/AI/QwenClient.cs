using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

public class QwenClient : ITextToTextClient
{
    private readonly HttpClient _http;
    private readonly ILogger<QwenClient> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public QwenClient(string apiUrl, string apiKey, string model, ILogger<QwenClient> logger)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiUrl.TrimEnd('/')) };
        _http.Timeout = TimeSpan.FromMinutes(2);
        _apiKey = apiKey;
        _model = model;
        _logger = logger;
        Capability = AiCapability.TextToText;
    }

    public AiCapability Capability { get; set; }

    public async Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Qwen API Key 未配置");
            return "";
        }

        var body = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/chat/completions");
        req.Content = JsonContent.Create(body);
        req.Headers.Authorization = new("Bearer", _apiKey);

        try
        {
            var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<QwenResponse>(cancellationToken: ct);
            return data?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qwen 调用失败");
            return "";
        }
    }

    private class QwenResponse
    {
        public List<QwenChoice>? Choices { get; set; }
    }

    private class QwenChoice
    {
        public QwenMsg? Message { get; set; }
    }

    private class QwenMsg
    {
        public string? Content { get; set; }
    }
}
