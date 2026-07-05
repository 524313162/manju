namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 08.ACE-MUSIC 音乐生成请求
/// </summary>
public class AceMusicRequest
{
    /// <summary>音乐风格/标签描述（必填）</summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>歌词文本（必填）</summary>
    public string Lyrics { get; set; } = string.Empty;

    /// <summary>时长（秒），默认 210</summary>
    public float? Duration { get; set; }

    /// <summary>随机种子，默认 0</summary>
    public int? Seed { get; set; }

    /// <summary>BPM 每分钟节拍数，默认 88</summary>
    public int? Bpm { get; set; }
}
