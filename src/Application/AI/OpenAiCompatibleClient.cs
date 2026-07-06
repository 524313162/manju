using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ManjuCraft.Application.AI;

/// <summary>
/// OpenAI 兼容协议客户端 (DeepSeek / OpenAI / Silicon)
/// </summary>
public class OpenAiCompatibleClient : IAiChatClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAiCompatibleClient(string apiUrl, string apiKey, string model)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiUrl) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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
        var response = await _http.PostAsync("/v1/chat/completions", content, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"OpenAI API error: {text}");

        var j = JsonDocument.Parse(text);
        return j.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? throw new Exception("API returned empty response");
    }
}
