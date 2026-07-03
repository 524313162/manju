using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ManjuCraft.Application.LLM.Providers;

public class OllamaClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaClient> _logger;
    private readonly string _apiUrl, _model;

    public OllamaClient(IHttpClientFactory httpFactory, IConfiguration config, ILogger<OllamaClient> logger)
    {
        _http = httpFactory.CreateClient();
        _logger = logger;
        var section = config.GetSection("Ai:Providers:Ollama");
        _apiUrl = section["ApiUrl"] ?? "http://localhost:11434";
        _model = section["Model"] ?? "qwen2.5";
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        var body = new { model = _model, messages = new[] {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userContent }
        }, stream = false };
        try
        {
            var res = await _http.PostAsJsonAsync(_apiUrl.TrimEnd('/') + "/api/chat", body, ct);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
            return data?.Message?.Content ?? "";
        }
        catch (Exception ex) { _logger.LogError(ex, "Ollama 调用失败"); return ""; }
    }

    class OllamaResponse { public Msg? Message { get; set; } }
    class Msg { public string? Content { get; set; } }
}