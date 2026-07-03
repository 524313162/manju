using Microsoft.Extensions.Logging;

namespace ManjuCraft.Application.LLM;

public class AiMediaService : IAiMediaService
{
    private readonly ILogger<AiMediaService> _logger;

    public AiMediaService(ILogger<AiMediaService> logger)
    {
        _logger = logger;
    }

    public Task<string> TextToImageAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("文生图: {Prompt}", prompt);
        // TODO: call ComfyUI Txt2Img workflow
        return Task.FromResult("/mock/images/txt2img.png");
    }

    public Task<string> GenerateImageAsync(string profile, string assetType, CancellationToken ct = default)
    {
        _logger.LogInformation("资产生图: {Type}", assetType);
        // TODO: call ComfyUI with asset type specific workflow
        return Task.FromResult($"/mock/images/{assetType.ToLower()}.png");
    }

    public Task<string> GenerateBgmAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("BGM生成: {Prompt}", prompt);
        // TODO: call ComfyUI audio workflow
        return Task.FromResult("/mock/audio/bgm.mp3");
    }

    public Task<string> GenerateVideoAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        _logger.LogInformation("视频生成: {Prompt}", prompt);
        // TODO: call ComfyUI Img2Video workflow
        return Task.FromResult("/mock/videos/video.mp4");
    }
}