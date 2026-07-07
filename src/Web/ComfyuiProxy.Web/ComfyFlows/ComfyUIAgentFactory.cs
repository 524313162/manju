namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// ComfyUI Agent 工厂
/// 根据工作流类型创建对应的 Agent 实例
/// </summary>
public class ComfyUIAgentFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ComfyUIAgentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 根据工作流类型获取对应的 Agent
    /// </summary>
    public object GetAgent(string workflowType)
    {
        return workflowType.ToLowerInvariant() switch
        {
            "zimage-text-to-image" => _serviceProvider.GetRequiredService<TextToImageAgent>(),
            "zimage-character-profile" => _serviceProvider.GetRequiredService<CharacterProfileAgent>(),
            "ltx-text-to-video" => _serviceProvider.GetRequiredService<TextToVideoAgent>(),
            "ltx-image-to-video" => _serviceProvider.GetRequiredService<ImageToVideoAgent>(),
            "hidream-storyboard" => _serviceProvider.GetRequiredService<StoryboardAgent>(),
            "ace-music-compose" => _serviceProvider.GetRequiredService<MusicComposeAgent>(),
            "stable-bgm-generate" => _serviceProvider.GetRequiredService<BgmGenerateAgent>(),
            "llm-qwen-execute" => _serviceProvider.GetRequiredService<LlmQwenAgent>(),
            _ => throw new ArgumentException($"未知的工作流类型: {workflowType}")
        };
    }
}
