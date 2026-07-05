namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 执行工作流请求
/// </summary>
public class WorkflowExecuteRequest
{
    /// <summary>
    /// 工作流文件名（不含路径，如 "01.ZIMAGE文生图.json"）
    /// </summary>
    public string WorkflowFileName { get; set; } = "";

    /// <summary>
    /// 参数键值对，key 对应工作流定义中的参数名
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// 客户端ID，不传则自动生成
    /// </summary>
    public string? ClientId { get; set; }
}
