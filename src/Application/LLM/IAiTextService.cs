namespace ManjuCraft.Application.LLM;

public interface IAiTextService
{
    Task<string> GenerateStoryAsync(string title, string prompt, CancellationToken ct = default);
    Task<string> RewriteStoryAsync(string prompt, string originalStory, CancellationToken ct = default);
    Task<string> ExtractAssetsAsync(string story, CancellationToken ct = default);
    Task<string> CreateCharacterProfileAsync(string characterDescription, CancellationToken ct = default);
    Task<string> CreateSceneProfileAsync(string sceneDescription, CancellationToken ct = default);
    Task<string> CreatePropProfileAsync(string propDescription, CancellationToken ct = default);
    Task<string> CreateSkillProfileAsync(string skillDescription, CancellationToken ct = default);
    Task<string> CreateBgmPromptAsync(string bgmDescription, CancellationToken ct = default);
    Task<string> CreateVideoPromptAsync(string dynamicDescription, List<string>? referenceImages = null, CancellationToken ct = default);
}