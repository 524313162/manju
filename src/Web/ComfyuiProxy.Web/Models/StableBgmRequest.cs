namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 09.STABLE-BGM 背景音乐请求
/// </summary>
public class StableBgmRequest
{
    /// <summary>简短音乐描述（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>时长（秒），默认 150</summary>
    public float? Duration { get; set; }

    /// <summary>分类：Music / Instrument / SFX / One-shot，默认 Music</summary>
    public string? Category { get; set; }
}
