namespace ComfyuiProxy.Web.Models;

/// <summary>
/// QWen 图生图工作流响应结果
/// </summary>
public class QwenImageEditResponse
{
    /// <summary>生成的图片 URL 列表</summary>
    public List<string> ImageUrls { get; set; } = new();
}
