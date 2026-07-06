namespace ComfyuiProxy.Web.Models;

/// <summary>
/// LTX 视频生成 响应（文生视频/图生视频共用）
/// </summary>
public class LtxVideoResponse
{
    /// <summary>ComfyUI 提示词 ID</summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>生成的视频 URL 列表</summary>
    public List<string> VideoUrls { get; set; } = new();

    /// <summary>执行耗时（毫秒）</summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; } = true;

    /// <summary>错误信息（失败时）</summary>
    public string? Error { get; set; }
}
