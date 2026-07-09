namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 20.LLM-QWen 大语言模型请求
/// </summary>
public class LlmQwenRequestDto
{
    /// <summary>提示词参数（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>最大生成长度，默认 32768</summary>
    public int? MaxLength { get; set; } = 32768;
}
