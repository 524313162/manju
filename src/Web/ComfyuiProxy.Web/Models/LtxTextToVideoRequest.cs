namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 03.LTX文生视频 请求
/// </summary>
public class LtxTextToVideoRequestDto
{
    /// <summary>提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    private int _width = 1280;
    private int _height = 720;

    /// <summary>视频宽度，默认 1280（≤0 时使用默认值）</summary>
    public int Width
    {
        get => _width;
        set => _width = value > 0 ? value : 1280;
    }

    /// <summary>视频高度，默认 720（≤0 时使用默认值）</summary>
    public int Height
    {
        get => _height;
        set => _height = value > 0 ? value : 720;
    }

    /// <summary>时长（秒），默认 5</summary>
    public int Duration { get; set; } = 5;

    /// <summary>帧率，默认 25</summary>
    public int Fps { get; set; } = 25;
}
