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

    private int _width = 1792;
    private int _height = 1024;

    /// <summary>生成宽度，默认 1792（小于等于 0 时使用默认值）</summary>
    public int Width
    {
        get => _width;
        set => _width = value > 0 ? value : 1792;
    }

    /// <summary>生成高度，默认 1024（小于等于 0 时使用默认值）</summary>
    public int Height
    {
        get => _height;
        set => _height = value > 0 ? value : 1024;
    }
}
