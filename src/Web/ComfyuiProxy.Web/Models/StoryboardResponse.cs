namespace ComfyuiProxy.Web.Models;

/// <summary>
/// HIDREAM 分镜 响应
/// </summary>
public class StoryboardResponse
{
    /// <summary>生成的图片 URL 列表</summary>
    public List<string> ImageUrls { get; set; } = new();
}
