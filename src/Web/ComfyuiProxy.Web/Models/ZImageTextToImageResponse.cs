namespace ComfyuiProxy.Web.Models;

/// <summary>
/// ZIMAGE 文生图 响应
/// </summary>
public class ZImageTextToImageResponse
{
    /// <summary>生成的图片 URL 列表</summary>
    public List<string> ImageUrls { get; set; } = new();
}
