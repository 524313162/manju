using System.Text.Json;
using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// ComfyUI Agent 基类
/// 提供公共的工作流执行逻辑：加载模板、注入参数、提交执行、等待结果、解析输出
/// </summary>
public abstract class ComfyUIAgentBase<TParams> : IComfyUIAgent
    where TParams : class
{
    protected readonly ComfyuiProxyService _proxyService;
    protected readonly ILogger _logger;

    protected ComfyUIAgentBase(ComfyuiProxyService proxyService, ILogger logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    /// <summary>工作流类型标识</summary>
    public abstract string WorkflowType { get; }

    /// <summary>工作流文件名</summary>
    public abstract string WorkflowFileName { get; }

    /// <summary>
    /// 构建工作流 JSON（加载模板 + 注入参数）
    /// 子类必须实现此方法，直接返回 API 格式的 JSON 字符串
    /// </summary>
    protected abstract Task<string> BuildWorkflowJsonAsync(TParams dto);

    /// <summary>
    /// 执行工作流的完整流程：构建 workflowJson → 提交执行 → 等待结果 → 解析输出
    /// </summary>
    /// <param name="parameters">请求参数（各 Agent 对应的 Request DTO）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        object parameters,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new WorkflowExecutionResult();

        try
        {
            // 1. 构建工作流 JSON（加载模板 + 注入参数 + 转 API 格式）
            var workflowJson = await BuildWorkflowJsonAsync((TParams)parameters);

            // 2. 提交到 ComfyUI 执行
            _logger.LogInformation("[{WorkflowType}] 提交工作流到 ComfyUI", WorkflowType);
            var promptId = await _proxyService.ExecuteWorkflowAsync(workflowJson);
            result.PromptId = promptId;

            // 3. 等待执行完成
            _logger.LogInformation("[{WorkflowType}] 等待执行完成, prompt_id: {PromptId}", WorkflowType, promptId);
            var historyItem = await _proxyService.WaitForResultAsync(promptId, cancellationToken: cancellationToken);
            if (historyItem == null)
            {
                result.Success = false;
                result.Error = "工作流执行超时";
                return result;
            }

            // 4. 解析输出
            ParseOutputs(historyItem, result);

            result.Success = true;
            result.ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("[{WorkflowType}] 工作流执行成功, 耗时: {Time:F0}ms",
                WorkflowType, result.ExecutionTimeMs);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "请求被取消";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{WorkflowType}] 工作流执行异常: {Message}", WorkflowType, ex.Message);
            result.Success = false;
            result.Error = ex.Message;
            result.ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 解析历史记录中的输出到结果对象
    /// 子类可重写此方法实现自定义解析逻辑
    /// </summary>
    protected virtual void ParseOutputs(JsonObject historyItem, WorkflowExecutionResult result)
    {
        // 默认解析图片、视频、音频、文本
        result.ImageUrls = _proxyService.ExtractImageUrls(historyItem);
        result.VideoUrls = _proxyService.ExtractVideoUrls(historyItem);
        result.AudioUrls = _proxyService.ExtractAudioUrls(historyItem);
        result.TextOutputs = _proxyService.ExtractTextOutputs(historyItem);
    }
}

/// <summary>
/// 工作流执行结果
/// </summary>
public class WorkflowExecutionResult
{
    public string PromptId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public double ExecutionTimeMs { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public List<string> VideoUrls { get; set; } = new();
    public List<string> AudioUrls { get; set; } = new();
    public List<string> TextOutputs { get; set; } = new();
}
