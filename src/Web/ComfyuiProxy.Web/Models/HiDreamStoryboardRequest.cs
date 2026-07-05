namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 07.HIDREAM分镜 请求
/// </summary>
public class HiDreamStoryboardRequest
{
    /// <summary>用户提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>是否切换到图片编辑模式，默认 false</summary>
    public bool? SwitchToImageEdit { get; set; }

    /// <summary>是否启用提示词优化，默认 false</summary>
    public bool? EnablePromptRefine { get; set; }
}
