namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 01.ZIMAGE文生图 请求
/// </summary>
public class ZImageTextToImageRequest
{
    /// <summary>正面提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>图像宽度，默认 1024</summary>
    public int? Width { get; set; }

    /// <summary>图像高度，默认 1024</summary>
    public int? Height { get; set; }

    /// <summary>随机种子，默认 0（随机）</summary>
    public int? Seed { get; set; }

    /// <summary>采样步数，默认 8</summary>
    public int? Steps { get; set; }
}
