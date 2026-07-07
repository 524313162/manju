namespace ComfyuiProxy.Web.Models;

/// <summary>
/// LTX 视频生成 响应（文生视频/图生视频共用）
/// </summary>
public class LtxVideoResponse
{
    /// <summary>生成的视频 URL 列表</summary>
    public List<string> VideoUrls { get; set; } = new();
}
