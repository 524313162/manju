using System.Text.Json.Nodes;

namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 通用工作流提交请求
/// </summary>
public class WorkflowSubmitRequestDto
{
    /// <summary>工作流类型标识（如 "ltx-text-to-video"）</summary>
    public string WorkflowType { get; set; } = string.Empty;

    /// <summary>Agent 对应的请求参数 DTO（JSON 对象）</summary>
    public JsonObject Parameters { get; set; } = new();
}
