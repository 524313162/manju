using System.Text;
using System.Text.Json;

namespace ManjuCraft.Application.AI;

/// <summary>
/// Dashscope / Qwen 客户端
/// </summary>
public class DashscopeClient : IAiChatClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

public DashscopeClient(string apiUrl, string apiKey, string model)
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
            input = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            },
            parameters = new { stream = false }
        });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        _http.DefaultRequestHeaders.Add("X-DashScope-API-Key", _apiKey);
        var response = await _http.PostAsync("/api/v1/services/aigc/text-generation/generation", content, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Dashscope API error: {text}");

        var j = JsonDocument.Parse(text);
        return j.RootElement.GetProperty("output").GetProperty("text").GetString()
            ?? throw new Exception("API returned empty response");
    }
}
