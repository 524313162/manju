using ComfyuiProxy.Web.ComfyFlows;

namespace ComfyuiProxy.Web.Models;

/// <summary>
/// STABLE-BGM 背景音乐 响应
/// </summary>
public class StableBgmResponse : ComfyUIResponseBase
{
    /// <summary>生成的音频 URL 列表</summary>
    public List<string> AudioUrls { get; set; } = new();
}
