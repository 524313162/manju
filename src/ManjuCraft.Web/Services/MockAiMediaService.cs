using ManjuCraft.Application.Service;

namespace ManjuCraft.Web.Services;

public class MockAiMediaService : IAiMediaService
{
    public async Task<string> TextToImageAsync(string prompt, CancellationToken ct = default)
    {
        await Task.Delay(1500, ct);
        return $"/mock/images/txt2img_{Guid.NewGuid():N}.png";
    }

    public async Task<string> GenerateImageAsync(string profile, string assetType, CancellationToken ct = default)
    {
        await Task.Delay(2000, ct);
        return $"/mock/images/{assetType.ToLower()}_{Guid.NewGuid():N}.png";
    }

    public async Task<string> GenerateBgmAsync(string prompt, CancellationToken ct = default)
    {
        await Task.Delay(3000, ct);
        return $"/mock/audio/bgm_{Guid.NewGuid():N}.mp3";
    }

    public async Task<string> GenerateVideoAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        await Task.Delay(5000, ct);
        return $"/mock/videos/video_{Guid.NewGuid():N}.mp4";
    }
}