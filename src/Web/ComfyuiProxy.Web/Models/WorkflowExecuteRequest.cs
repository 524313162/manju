namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 工作流执行请求
/// </summary>
public class WorkflowExecuteRequest
{
    /// <summary>工作流文件名，如 "20.LLM-QWen.json"</summary>
    public string WorkflowFileName { get; set; } = string.Empty;

    /// <summary>要注入的参数，key 为参数名，value 为参数值</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}
