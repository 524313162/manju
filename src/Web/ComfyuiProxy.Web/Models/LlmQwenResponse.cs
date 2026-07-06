using ComfyuiProxy.Web.ComfyFlows;

namespace ComfyuiProxy.Web.Models;

/// <summary>
/// LLM-QWen 大语言模型 响应
/// </summary>
public class LlmQwenResponse : ComfyUIResponseBase
{
    /// <summary>生成的文本内容</summary>
    public string Text { get; set; } = string.Empty;
}
