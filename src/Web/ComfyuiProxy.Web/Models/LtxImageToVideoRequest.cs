namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 04.LTX图生视频 请求
/// </summary>
public class LtxImageToVideoRequest
{
    /// <summary>起始图片路径（必填）</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;
}
