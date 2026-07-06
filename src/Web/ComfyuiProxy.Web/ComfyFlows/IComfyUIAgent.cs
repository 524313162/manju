using System.Text.Json.Nodes;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// ComfyUI 工作流 Agent 接口
/// 每个工作流类型对应一个 Agent，负责加载工作流模板、注入参数、执行并解析结果
/// </summary>
public interface IComfyUIAgent
{
    /// <summary>获取工作流类型标识</summary>
    string WorkflowType { get; }

    /// <summary>获取工作流文件名（如 "01.ZIMAGE-文生图.json"）</summary>
    string WorkflowFileName { get; }

    /// <summary>
    /// 将请求参数注入到工作流 JSON 模板（UI 格式，包含 nodes 数组）中
    /// 子类应遍历 workflow["nodes"] 数组，按 type 匹配节点，修改 widgets_values
    /// </summary>
    /// <param name="workflow">UI 格式的工作流对象 { nodes: [...], ... }</param>
    /// <param name="parameters">请求参数字典</param>
    void InjectParameters(JsonObject workflow, Dictionary<string, object> parameters);

    /// <summary>
    /// 执行工作流的完整流程
    /// </summary>
    /// <param name="parameters">请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<WorkflowExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
