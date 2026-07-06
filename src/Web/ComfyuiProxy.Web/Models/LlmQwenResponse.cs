namespace ComfyuiProxy.Web.Models;

/// <summary>
/// LLM-QWen 大语言模型 响应
/// </summary>
public class LlmQwenResponse
{
    /// <summary>ComfyUI 提示词 ID</summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>生成的文本内容</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>执行耗时（毫秒）</summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; } = true;

    /// <summary>错误信息（失败时）</summary>
    public string? Error { get; set; }
}
