using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

/// <summary>
/// AI 客户端工厂 — 根据 provider.Name 路由到对应实现
/// </summary>
public interface IAiChatClientFactory
{
    IAiChatClient? Create(ApiProvider provider);
}

public class AiChatClientFactory : IAiChatClientFactory
{
    public IAiChatClient? Create(ApiProvider provider)
    {
        var name = (provider.Name ?? "").ToLowerInvariant();

        if (name.Contains("deepseek") || name.Contains("openai") || name.Contains("silicon"))
            return new OpenAiCompatibleClient(provider.ApiUrl, provider.ApiKey, provider.Model);

        if (name.Contains("volcengine") || name.Contains("doubao"))
            return new VolcengineClient(provider.ApiUrl, provider.ApiKey, provider.Model);

        if (name.Contains("dashscope") || name.Contains("qwen") || name.Contains("aliyun"))
            return new DashscopeClient(provider.ApiUrl, provider.ApiKey, provider.Model);

        if (name.Contains("gemini") || name.Contains("google"))
            return new GeminiClient(provider.ApiUrl, provider.ApiKey, provider.Model);

        if (name.Contains("ollama"))
            return new OllamaClient(provider.ApiUrl, provider.Model);

        // 默认按 OpenAI 兼容协议
        return new OpenAiCompatibleClient(provider.ApiUrl, provider.ApiKey, provider.Model);
    }
}
