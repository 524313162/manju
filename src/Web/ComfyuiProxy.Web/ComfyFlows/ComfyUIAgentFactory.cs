namespace ComfyuiProxy.Web.ComfyFlows;

public class ComfyUIAgentFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ComfyUIAgentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object GetAgent(string workflowType)
    {
        return workflowType.ToLowerInvariant() switch
        {
            "zimage-text-to-image" => _serviceProvider.GetRequiredService<ZImageTextToImageAgent>(),
            "zimage-character-profile" => _serviceProvider.GetRequiredService<ZImageCharacterProfileAgent>(),
            "ltx-text-to-video" => _serviceProvider.GetRequiredService<LtxTextToVideoAgent>(),
            "ltx-image-to-video" => _serviceProvider.GetRequiredService<LtxImageToVideoAgent>(),
            "hidream-storyboard" => _serviceProvider.GetRequiredService<HiDreamStoryboardAgent>(),
            "ace-music-compose" => _serviceProvider.GetRequiredService<AceMusicAgent>(),
            "stable-bgm-generate" => _serviceProvider.GetRequiredService<StableBgmAgent>(),
            "llm-qwen-execute" => _serviceProvider.GetRequiredService<LlmQwenAgent>(),
            _ => throw new ArgumentException($"未知的工作流类型: {workflowType}")
        };
    }
}
