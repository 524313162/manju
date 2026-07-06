namespace ComfyuiProxy.Web.Models;

/// <summary>
/// ZIMAGE 人物档案 响应
/// </summary>
public class CharacterProfileResponse
{
    /// <summary>ComfyUI 提示词 ID</summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>生成的人物图片 URL 列表</summary>
    public List<string> ImageUrls { get; set; } = new();

    /// <summary>执行耗时（毫秒）</summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; } = true;

    /// <summary>错误信息（失败时）</summary>
    public string? Error { get; set; }
}
