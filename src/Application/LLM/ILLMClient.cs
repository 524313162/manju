namespace ManjuCraft.Application.LLM;

public interface ILLMClient
{
    Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken ct = default);
}