namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 07.HIDREAM分镜 请求
/// </summary>
public class HiDreamStoryboardRequestDto
{
    /// <summary>提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>参考图片路径（可选）</summary>
    public string? ImagePath { get; set; }
}
