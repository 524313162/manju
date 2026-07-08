using ManjuCraft.Application.Service.ComfyuiProxy;

namespace ManjuCraft.Application.Service;

public interface IComfyuiProxyService
{
    Task<(string? promptId, TResult? result)> SubmitAndPollAsync<TResult>(
        string proxyEndpoint,
        object payload,
        string workflowType,
        int pollIntervalMs = 5000,
        int timeoutMs = 600000,
        CancellationToken cancellationToken = default) where TResult : new();

    Task<(bool success, TResult? result, string? message)> GetResultAsync<TResult>(
        string promptId,
        string workflowType,
        CancellationToken cancellationToken = default) where TResult : new();

    Task<(bool success, string? message)> InterruptAsync(
        CancellationToken cancellationToken = default);

    string GetProxyUrl();
}
