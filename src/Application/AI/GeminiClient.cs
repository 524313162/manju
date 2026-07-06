using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ManjuCraft.Application.AI;

/// <summary>
/// Gemini (Google) 客户端 — 使用 Google 提供的 OpenAI 兼容端点
/// </summary>
public class GeminiClient : IAiChatClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiClient(string apiUrl, string apiKey, string model)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiUrl) };
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
        _http.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
        var response = await _http.PostAsync("/v1beta/openai/chat/completions", content, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Gemini API error: {text}");

        var j = JsonDocument.Parse(text);
        return j.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? throw new Exception("API returned empty response");
    }
}
