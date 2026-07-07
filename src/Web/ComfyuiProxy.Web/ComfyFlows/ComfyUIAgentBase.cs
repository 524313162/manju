using System.Text.Json;
using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public abstract class ComfyUIAgentBase<TParams, TResult> : IComfyUIAgent<TParams, TResult>
    where TParams : class
    where TResult : new()
{
    protected readonly ComfyuiProxyService _proxyService;
    protected readonly ILogger _logger;

    protected ComfyUIAgentBase(ComfyuiProxyService proxyService, ILogger logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public abstract string WorkflowType { get; }

    public abstract string WorkflowFileName { get; }

    protected abstract Task<string> BuildWorkflowJsonAsync(TParams dto);

    /// <summary>
    /// 构建并提交工作流，返回 promptId（不等待执行完成）
    /// 所有控制器应调用此方法，然后通过 GET /api/comfyui/result/{promptId} 轮询结果
    /// </summary>
    public async Task<string> SubmitAsync(TParams parameters)
    {
        var workflowJson = await BuildWorkflowJsonAsync(parameters);

        var payload = new JsonObject
        {
            ["prompt"] = JsonNode.Parse(workflowJson)
        };
        var payloadJson = payload.ToJsonString();

        _logger.LogInformation("[{WorkflowType}] 提交工作流到 ComfyUI", WorkflowType);
        return await _proxyService.ExecuteWorkflowAsync(payloadJson);
    }

    /// <summary>
    /// 解析历史记录中的输出到结果对象
    /// </summary>
    public TResult ParseHistory(JsonObject historyItem)
    {
        var result = new TResult();
        ParseOutputs(historyItem, result);
        return result;
    }

    /// <summary>
    /// 解析历史记录中的输出到结果对象
    /// 子类必须实现此方法，从 historyItem 中提取输出并填充到 result 中
    /// </summary>
    protected abstract void ParseOutputs(JsonObject historyItem, TResult result);
}
