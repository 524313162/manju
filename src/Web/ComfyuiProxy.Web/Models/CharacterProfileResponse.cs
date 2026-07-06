using ComfyuiProxy.Web.ComfyFlows;

namespace ComfyuiProxy.Web.Models;

/// <summary>
/// ZIMAGE 人物档案 响应
/// </summary>
public class CharacterProfileResponse : ComfyUIResponseBase
{
    /// <summary>生成的人物图片 URL 列表</summary>
    public List<string> ImageUrls { get; set; } = new();
}
