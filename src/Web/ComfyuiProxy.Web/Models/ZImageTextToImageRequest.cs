namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 01.ZIMAGE文生图 请求
/// </summary>
public class ZImageTextToImageRequest
{
    /// <summary>提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>图像宽度，默认 16:9 比例宽（如 1024）</summary>
    public int? Width { get; set; }

    /// <summary>图像高度，默认 16:9 比例高（如 576）</summary>
    public int? Height { get; set; }
}
