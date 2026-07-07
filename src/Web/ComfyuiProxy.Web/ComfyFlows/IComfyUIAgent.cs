using System.Text.Json.Nodes;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// ComfyUI 工作流 Agent 泛型接口
/// </summary>
public interface IComfyUIAgent<TParams, TResult>
    where TParams : class
    where TResult : new()
{
    string WorkflowType { get; }

    Task<string> SubmitAsync(TParams parameters);

    TResult ParseHistory(JsonObject historyItem);
}
