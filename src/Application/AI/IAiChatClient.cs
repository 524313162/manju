using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.AI;

/// <summary>
/// 大模型客户端接口 — 所有厂商统一契约
/// </summary>
public interface IAiChatClient
{
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
