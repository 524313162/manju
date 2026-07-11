using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ManjuCraft.Application.AI;

/// <summary>
/// Ollama 客户端
/// </summary>
public class OllamaClient : IAiChatClient
{
    private readonly HttpClient _http;
    private readonly string _model;

public OllamaClient(string apiUrl, string model)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = Timeout.InfiniteTimeSpan };
        _model = model;
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            stream = false
        });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/chat", content, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Ollama API error: {text}");

        var j = JsonDocument.Parse(text);
        return j.RootElement.GetProperty("message").GetProperty("content").GetString()
            ?? throw new Exception("API returned empty response");
    }
}
