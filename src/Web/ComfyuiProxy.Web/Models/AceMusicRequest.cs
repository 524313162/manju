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
}
