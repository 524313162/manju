namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 04.LTX图生视频 请求
/// </summary>
public class LtxImageToVideoRequest
{
    /// <summary>视频提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>起始图片的本地文件路径（必填）</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>视频宽度，默认 768</summary>
    public int? Width { get; set; }

    /// <summary>视频高度，默认 512</summary>
    public int? Height { get; set; }

    /// <summary>视频时长（秒），默认 5</summary>
    public float? Duration { get; set; }

    /// <summary>帧率，默认 24</summary>
    public int? Fps { get; set; }

    /// <summary>随机种子，默认 0（随机）</summary>
    public int? Seed { get; set; }

    /// <summary>是否启用提示词增强，默认 true</summary>
    public bool? PromptEnhance { get; set; }
}
