namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 01.ZIMAGE文生图 请求
/// </summary>
public class ZImageTextToImageRequestDto
{
    /// <summary>提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    private int _width = 1024;
    private int _height = 768;

    /// <summary>图像宽度，默认 1024（小于等于 0 时使用默认值）</summary>
    public int Width
    {
        get => _width;
        set => _width = value > 0 ? value : 1024;
    }

    /// <summary>图像高度，默认 768（小于等于 0 时使用默认值）</summary>
    public int Height
    {
        get => _height;
        set => _height = value > 0 ? value : 768;
    }
}
