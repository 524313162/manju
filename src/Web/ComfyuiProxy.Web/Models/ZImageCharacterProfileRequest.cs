namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 02.ZIMAGE人物档案 请求
/// </summary>
public class ZImageCharacterProfileRequestDto
{
    /// <summary>系统提示词（必填）</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>人物提示词（必填）</summary>
    public string CharacterPrompt { get; set; } = string.Empty;

    /// <summary>反向提示词（必填）</summary>
    public string NegativePrompt { get; set; } = string.Empty;
}
