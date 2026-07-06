namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 03.LTX文生视频 请求
/// </summary>
public class LtxTextToVideoRequestDto
{
    /// <summary>提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;
}
