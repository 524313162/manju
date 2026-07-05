namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 20.LLM-QWen 大语言模型请求
/// </summary>
public class LlmQwenRequest
{
    /// <summary>提示词/系统提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>最大生成长度，默认 2048</summary>
    public int? MaxLength { get; set; }
}
