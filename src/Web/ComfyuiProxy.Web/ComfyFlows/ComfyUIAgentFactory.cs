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
    public IComfyUIAgent GetAgent(string workflowType)
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

    /// <summary>
    /// 获取所有已注册的 Agent 类型列表
    /// </summary>
    public IEnumerable<(string Type, string FileName)> GetAllAgentTypes()
    {
        yield return ("zimage-text-to-image", "01.ZIMAGE-文生图.json");
        yield return ("zimage-character-profile", "02.ZIMAGE-人物档案.json");
        yield return ("ltx-text-to-video", "03.LTX-文生视频.json");
        yield return ("ltx-image-to-video", "04.LTX-图生视频.json");
        yield return ("hidream-storyboard", "07.HIDREAM-分镜.json");
        yield return ("ace-music-compose", "08.ACE-MUSIC-音乐生成.json");
        yield return ("stable-bgm-generate", "09.STABLE-BGM-背景音乐.json");
        yield return ("llm-qwen-execute", "20.LLM-QWen.json");
    }
}
