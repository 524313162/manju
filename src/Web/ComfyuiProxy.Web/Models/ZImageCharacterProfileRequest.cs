namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 02.ZIMAGE人物档案 请求
/// </summary>
public class ZImageCharacterProfileRequest
{
    /// <summary>角色正面提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>版面/布局提示词（必填）</summary>
    public string LayoutPrompt { get; set; } = string.Empty;

    /// <summary>反向提示词（必填）</summary>
    public string NegativePrompt { get; set; } = string.Empty;
}
