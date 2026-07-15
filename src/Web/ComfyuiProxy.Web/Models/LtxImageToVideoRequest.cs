namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 04.LTX图生视频 请求
/// </summary>
public class LtxImageToVideoRequestDto
{
    /// <summary>起始图片路径（必填）</summary>
    public string ImagePath { get; set; } = string.Empty;

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

    /// <summary>时长（秒），默认 3</summary>
    public int Duration { get; set; } = 3;

    /// <summary>帧率，默认 25</summary>
    public int Fps { get; set; } = 25;

    /// <summary>随机种子（默认 -1 表示随机）</summary>
    public long Seed { get; set; } = -1;
}
