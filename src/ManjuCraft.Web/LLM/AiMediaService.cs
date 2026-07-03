using Microsoft.Extensions.Logging;
using ManjuCraft.Application.LLM;

namespace ManjuCraft.Web.LLM;

public class AiMediaService : IAiMediaService
{
    private readonly ILogger<AiMediaService> _logger;

    public AiMediaService(ILogger<AiMediaService> logger)
    {
        _logger = logger;
    }

    public async Task<string> TextToImageAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("文生图请求: {Prompt}", prompt);
        await Task.Delay(1500, ct);
        return $"/mock/images/txt2img_{Guid.NewGuid():N}.png";
    }

    public async Task<string> GenerateImageAsync(string profile, string assetType, CancellationToken ct = default)
    {
        _logger.LogInformation("资产生图请求 - 类型: {Type}, 档案: {Profile}", assetType, profile);
        await Task.Delay(2000, ct);
        return $"/mock/images/{assetType.ToLower()}_{Guid.NewGuid():N}.png";
    }

    public async Task<string> GenerateBgmAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("BGM生成请求: {Prompt}", prompt);
        await Task.Delay(3000, ct);
        return $"/mock/audio/bgm_{Guid.NewGuid():N}.mp3";
    }

    public async Task<string> GenerateVideoAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        _logger.LogInformation("视频生成请求: {Prompt}, 参考图: {Count}", prompt, referenceImages?.Count ?? 0);
        await Task.Delay(5000, ct);
        return $"/mock/videos/video_{Guid.NewGuid():N}.mp4";
    }
}