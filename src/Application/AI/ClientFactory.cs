using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

/// <summary>
/// AI 客户端注册表实现
/// 根据 ApiProvider 的名称识别提供商类型，返回对应的客户端
/// </summary>
public class AiClientRegistry : IAiClientRegistry
{
    private readonly IServiceProvider _services;

    public AiClientRegistry(IServiceProvider services)
    {
        _services = services;
    }

    public ITextToTextClient? GetTextToTextClient(ApiProvider provider)
    {
        var loggerFactory = _services.GetRequiredService<ILoggerFactory>();
        var name = (provider.Name ?? "").ToLowerInvariant();

        if (name.Contains("deepseek"))
        {
            var logger = loggerFactory.CreateLogger<DeepSeekClient>();
            return new DeepSeekClient(provider.ApiUrl, provider.ApiKey, provider.Model ?? "deepseek-chat", logger)
            { Capability = AiCapability.TextToText };
        }

        if (name.Contains("volcengine") || name.Contains("doubao"))
        {
            var logger = loggerFactory.CreateLogger<VolcengineClient>();
            return new VolcengineClient(provider.ApiUrl, provider.ApiKey, provider.Model ?? "doubao-1.5-pro", logger)
            { Capability = AiCapability.TextToText };
        }

        if (name.Contains("qwen") || name.Contains("dashscope") || name.Contains("aliyun"))
        {
            var logger = loggerFactory.CreateLogger<QwenClient>();
            return new QwenClient(provider.ApiUrl, provider.ApiKey, provider.Model ?? "qwen-plus", logger)
            { Capability = AiCapability.TextToText };
        }

        return null;
    }

    public ITextToImageClient? GetTextToImageClient(ApiProvider provider)
    {
        return null;
    }

    public ITextToAudioClient? GetTextToAudioClient(ApiProvider provider)
    {
        return null;
    }

    public ITextToVideoClient? GetTextToVideoClient(ApiProvider provider)
    {
        return null;
    }

    public IImageToVideoClient? GetImageToVideoClient(ApiProvider provider) => null;
}
