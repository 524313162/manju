namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 08.ACE-MUSIC 音乐生成请求
/// </summary>
public class AceMusicRequestDto
{
    /// <summary>提示词参数（音乐风格/描述，必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>歌词参数（必填）</summary>
    public string Lyrics { get; set; } = string.Empty;

    /// <summary>每秒节拍数，默认 88</summary>
    public int Bpm { get; set; } = 88;

    /// <summary>拍号，默认 4</summary>
    public string Timesignature { get; set; } = "4";

    /// <summary>歌词语言，默认 zh</summary>
    public string Language { get; set; } = "zh";

    /// <summary>调式/音阶，默认 E minor</summary>
    public string Keyscale { get; set; } = "E minor";

    /// <summary>生成时长（秒），为空则根据歌词长度自动计算</summary>
    public double? Seconds { get; set; }
}
