namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 工作流执行响应
/// </summary>
public class WorkflowExecuteResponse
{
    public string PromptId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public double ExecutionTimeMs { get; set; }
    public List<string> TextOutputs { get; set; } = new();
    public List<ImageOutputItem> ImageOutputs { get; set; } = new();
    public List<ImageOutputItem> AudioOutputs { get; set; } = new();
}

/// <summary>
/// 图片/GIF 输出项
/// </summary>
public class ImageOutputItem
{
    public string Filename { get; set; } = string.Empty;
    public string? Subfolder { get; set; }
    public string? Type { get; set; }
    public string? Url { get; set; }
}
