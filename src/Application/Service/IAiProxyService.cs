using ManjuCraft.Application.Service.ComfyuiProxy;
using System.Text.Json.Nodes;

namespace ManjuCraft.Application.Service;

/// <summary>
/// ComfyUI 代理统一服务接口
/// 封装所有 ComfyUI 调用的公共逻辑：提交工作流 → 轮询等待 → 获取结果
/// </summary>
public interface IAiProxyService
{
    /// <summary>
    /// 提交工作流到 ComfyUI 代理并轮询等待结果
    /// </summary>
    /// <typeparam name="TResult">输出结果类型</typeparam>
    /// <param name="proxyEndpoint">代理 API 端点（如 "api/comfyui/zimage/text-to-image"）</param>
    /// <param name="payload">提交参数</param>
    /// <param name="workflowType">工作流类型标识（用于后续查询结果）</param>
    /// <param name="pollIntervalMs">轮询间隔（毫秒），默认 5000</param>
    /// <param name="timeoutMs">超时时间（毫秒），默认 600000（10 分钟）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功返回 (promptId, result)；超时返回 (promptId, null)</returns>
    Task<(string? promptId, TResult? result)> SubmitAndPollAsync<TResult>(
        string proxyEndpoint,
        object payload,
        string workflowType,
        int pollIntervalMs = 5000,
        int timeoutMs = 600000,
        CancellationToken cancellationToken = default) where TResult : new();

    /// <summary>
    /// 通过 promptId 查询 ComfyUI 代理的任务结果
    /// </summary>
    /// <typeparam name="TResult">输出结果类型</typeparam>
    /// <param name="promptId">任务 ID</param>
    /// <param name="workflowType">工作流类型标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功返回 (true, result, null)；失败返回 (false, null, message)</returns>
    Task<(bool success, TResult? result, string? message)> GetResultAsync<TResult>(
        string promptId,
        string workflowType,
        CancellationToken cancellationToken = default) where TResult : new();

    /// <summary>
    /// 中断 ComfyUI 代理当前正在执行的任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>(success, message)</returns>
    Task<(bool success, string? message)> InterruptAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// LLM 大模型文本生成（同步模式）
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="maxLength">最大生成长度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>(true, text, null) 或 (false, null, message)</returns>
    Task<(bool success, string? text, string? message)> ChatAsync(
        string prompt,
        int? maxLength = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// LLM 大模型文本生成（支持系统提示词）
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userMessage">用户消息</param>
    /// <param name="maxLength">最大生成长度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>(true, text, null) 或 (false, null, message)</returns>
    Task<(bool success, string? text, string? message)> ChatAsync(
        string systemPrompt,
        string userMessage,
        int? maxLength = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 ComfyUI 代理的基础 URL
    /// </summary>
    string GetProxyUrl();
}
