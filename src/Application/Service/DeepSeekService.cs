// @name:         DeepSeekService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  DeepSeek LLM 服务实现
// @version:      1.0
// @date:         2026-07-01

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// DeepSeek 配置
    /// </summary>
    public class DeepSeekOptions
    {
        public const string SectionName = "DeepSeek";
        public string ApiKey { get; set; } = "";
        public string ApiUrl { get; set; } = "https://api.deepseek.com";
        public string Model { get; set; } = "deepseek-chat";
    }

    /// <summary>
    /// DeepSeek LLM 服务实现
    /// </summary>
    public class DeepSeekService : IDeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DeepSeekService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly string _model;

        public DeepSeekService(
            IHttpClientFactory httpClientFactory,
            IOptionsSnapshot<DeepSeekOptions> options,
            ILogger<DeepSeekService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _apiKey = options.Value.ApiKey;
            _apiUrl = options.Value.ApiUrl;
            _model = options.Value.Model;
        }

        public async Task<string> ChatAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("DeepSeek API Key 未配置");
                return "";
            }

            var requestBody = new
            {
                model = _model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                stream = false
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl.TrimEnd('/') + "/chat/completions");
            request.Content = JsonContent.Create(requestBody);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<DeepSeekResponse>();
                return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeepSeek API 调用失败");
                return "";
            }
        }

        public async Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userContent }
            };
            return await ChatAsync(messages, cancellationToken);
        }
    }

    internal class DeepSeekResponse
    {
        public List<Choice> Choices { get; set; } = new();
    }

    internal class Choice
    {
        public ChatMessageDto Message { get; set; } = new();
    }

    internal class ChatMessageDto
    {
        public string Content { get; set; } = "";
    }
}
