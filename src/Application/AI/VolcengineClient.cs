using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ManjuCraft.Application.AI;

/// <summary>
/// Volcengine / Doubao 客户端
/// </summary>
public class VolcengineClient : IAiChatClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

public VolcengineClient(string apiUrl, string apiKey, string model)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = Timeout.InfiniteTimeSpan };
        _apiKey = apiKey;
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
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        var response = await _http.PostAsync("/chat/completions", content, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Volcengine API error: {text}");

        var j = JsonDocument.Parse(text);
        return j.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? throw new Exception("API returned empty response");
    }
}
