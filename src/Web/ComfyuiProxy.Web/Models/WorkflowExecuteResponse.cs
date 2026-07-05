namespace ComfyuiProxy.Web.Models;

public class WorkflowExecuteResponse
{
    public string PromptId { get; set; } = "";
    public string Status { get; set; } = ""; // "completed", "error", "timeout"
    public List<WorkflowOutputFile> Outputs { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class WorkflowOutputFile
{
    public string FileName { get; set; } = "";
    public string Subfolder { get; set; } = "";
    public string Type { get; set; } = "";
    public string Url { get; set; } = ""; // 通过代理访问的完整 URL
}
