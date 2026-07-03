using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.AI;

public interface IAiMediaService
{
    Task<string> TextToImageAsync(string prompt, long? projectId = null, CancellationToken ct = default);
    Task<string> GenerateImageAsync(string profile, string assetType, long? projectId = null, CancellationToken ct = default);
    Task<string> GenerateBgmAsync(string prompt, long? projectId = null, CancellationToken ct = default);
    Task<string> GenerateVideoAsync(string prompt, List<string>? referenceImages = null, long? projectId = null, CancellationToken ct = default);
}

public class AiMediaService : IAiMediaService
{
    private readonly IProjectDbContext _db;
    private readonly IAiClientRegistry _registry;
    private readonly ILogger<AiMediaService> _logger;

    public AiMediaService(IProjectDbContext db, IAiClientRegistry registry, ILogger<AiMediaService> logger)
    {
        _db = db;
        _registry = registry;
        _logger = logger;
    }

    public async Task<string> TextToImageAsync(string prompt, long? projectId = null, CancellationToken ct = default)
        => await CallWithImageClient(AiCapability.TextToImage, async (client, ct2) => await client.GenerateAsync(prompt, ct2), ct);

    public async Task<string> GenerateImageAsync(string profile, string assetType, long? projectId = null, CancellationToken ct = default)
    {
        var p = $"Profile: {profile}, AssetType: {assetType}";
        return await CallWithImageClient(AiCapability.TextToImage, async (client, ct2) => await client.GenerateAsync(p, ct2), ct);
    }

    public async Task<string> GenerateBgmAsync(string prompt, long? projectId = null, CancellationToken ct = default)
    {
        var provider = await GetProviderAsync(AiCapability.TextToAudio, ct);
        if (provider == null) return "";

        _logger.LogInformation("使用 {Name} 执行 {Capability}", provider.Name, AiCapability.TextToAudio);

        var client = _registry.GetTextToAudioClient(provider);
        if (client == null)
        {
            _logger.LogWarning("未找到文本到音频的客户端实现: {Name}", provider.Name);
            return "";
        }

        try
        {
            return await client.GenerateBgmAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Name} 调用异常", provider.Name);
            return "";
        }
    }

    public async Task<string> GenerateVideoAsync(string prompt, List<string>? referenceImages = null, long? projectId = null, CancellationToken ct = default)
        => await CallWithVideoClient(AiCapability.TextToVideo, async (client, ct2) => await client.GenerateAsync(prompt, referenceImages, ct2), ct);

    private async Task<string> CallWithImageClient(AiCapability capability, Func<ITextToImageClient, CancellationToken, Task<string>> action, CancellationToken ct)
    {
        var provider = await GetProviderAsync(capability, ct);
        if (provider == null) return "";

        _logger.LogInformation("使用 {Name} 执行 {Capability}", provider.Name, capability);

        var client = _registry.GetTextToImageClient(provider);
        if (client == null)
        {
            _logger.LogWarning("未找到 {Capability} 的客户端实现: {Name}", capability, provider.Name);
            return "";
        }

        try
        {
            return await action(client, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Name} 调用异常", provider.Name);
            return "";
        }
    }

    private async Task<string> CallWithVideoClient(AiCapability capability, Func<ITextToVideoClient, CancellationToken, Task<string>> action, CancellationToken ct)
    {
        var provider = await GetProviderAsync(capability, ct);
        if (provider == null) return "";

        _logger.LogInformation("使用 {Name} 执行 {Capability}", provider.Name, capability);

        var client = _registry.GetTextToVideoClient(provider);
        if (client == null)
        {
            _logger.LogWarning("未找到 {Capability} 的客户端实现: {Name}", capability, provider.Name);
            return "";
        }

        try
        {
            return await action(client, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Name} 调用异常", provider.Name);
            return "";
        }
    }

    private async Task<ApiProvider?> GetProviderAsync(AiCapability capability, CancellationToken ct)
    {
        return await _db.ApiProviders
            .Where(p => p.Capability == capability)
            .FirstOrDefaultAsync(ct);
    }
}
