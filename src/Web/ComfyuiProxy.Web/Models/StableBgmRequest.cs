namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 09.STABLE-BGM 背景音乐请求
/// </summary>
public class StableBgmRequestDto
{
    /// <summary>提示词参数（音乐描述，必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>音频时长（秒），默认 150</summary>
    public float? Duration { get; set; }
}
