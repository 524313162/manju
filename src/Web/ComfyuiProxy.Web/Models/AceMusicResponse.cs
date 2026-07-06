using ComfyuiProxy.Web.ComfyFlows;

namespace ComfyuiProxy.Web.Models;

/// <summary>
/// ACE-MUSIC 音乐生成 响应
/// </summary>
public class AceMusicResponse : ComfyUIResponseBase
{
    /// <summary>生成的音频 URL 列表</summary>
    public List<string> AudioUrls { get; set; } = new();
}
