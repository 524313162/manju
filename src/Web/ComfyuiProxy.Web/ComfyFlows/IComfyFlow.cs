namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 工作流统一接口 — 每个工作流自己负责全部执行逻辑
/// </summary>
public interface IComfyFlow<TInput, TOutput>
{
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct = default);
}
