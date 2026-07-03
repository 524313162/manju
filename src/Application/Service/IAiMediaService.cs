namespace ManjuCraft.Application.Service;

public interface IAiMediaService
{
    Task<string> TextToImageAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateImageAsync(string profile, string assetType, CancellationToken ct = default);
    Task<string> GenerateBgmAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateVideoAsync(string prompt, List<string>? referenceImages = null, CancellationToken ct = default);
}