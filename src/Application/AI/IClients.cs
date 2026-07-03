using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

/// <summary>
/// 文本到文本 AI 客户端（大语言模型对话）
/// </summary>
public interface ITextToTextClient
{
    AiCapability Capability { get; }
    Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken ct = default);
}

/// <summary>
/// 文本到图片 AI 客户端
/// </summary>
public interface ITextToImageClient
{
    AiCapability Capability { get; }
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
}

/// <summary>
/// 文本到音频 AI 客户端
/// </summary>
public interface ITextToAudioClient
{
    AiCapability Capability { get; }
    Task<string> GenerateBgmAsync(string prompt);
}

/// <summary>
/// 文本到视频 AI 客户端
/// </summary>
public interface ITextToVideoClient
{
    AiCapability Capability { get; }
    Task<string> GenerateAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default);
}

/// <summary>
/// 图片到视频 AI 客户端
/// </summary>
public interface IImageToVideoClient
{
    AiCapability Capability { get; }
    Task<string> GenerateAsync(string prompt, string referenceImagePath, CancellationToken ct = default);
}

/// <summary>
/// AI 客户端注册表 — 根据 ApiProvider 获取对应能力的客户端实现
/// </summary>
public interface IAiClientRegistry
{
    ITextToTextClient? GetTextToTextClient(ApiProvider provider);
    ITextToImageClient? GetTextToImageClient(ApiProvider provider);
    ITextToAudioClient? GetTextToAudioClient(ApiProvider provider);
    ITextToVideoClient? GetTextToVideoClient(ApiProvider provider);
    IImageToVideoClient? GetImageToVideoClient(ApiProvider provider);
}
