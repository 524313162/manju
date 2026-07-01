// @name:         ComfyuiTaskPoller
// @author:       AI Assistant
// @namespace:    ManjuCraft.Infrastructure.Service
// @description:  ComfyUI任务轮询器
// @version:      1.0
// @date:         2026-06-30

using ManjuCraft.Domain.Models.ComfyUI;

namespace ManjuCraft.Infrastructure.Service;

/// <summary>
/// ComfyUI任务轮询器
/// </summary>
public class ComfyuiTaskPoller
{
    private readonly IComfyuiClient _client;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="client">ComfyUI客户端</param>
    public ComfyuiTaskPoller(IComfyuiClient client)
    {
        _client = client;
    }

    /// <summary>
    /// 轮询任务状态
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    /// <param name="promptId">提示词ID</param>
    /// <param name="token">取消令牌</param>
    public async Task<ComfyuiHistoryItem> PollAsync(string apiUrl, string promptId, CancellationToken token = default)
    {
        var retries = 30;
        while (retries-- > 0)
        {
            if (token.IsCancellationRequested) return null;
            var history = await _client.GetHistoryAsync(apiUrl, promptId);
            if (history != null) return history;
            history = new ComfyuiHistoryItem(new Dictionary<string, ComfyuiHistoryNodeOutputs>());
            return history;
        }
        return new ComfyuiHistoryItem(new Dictionary<string, ComfyuiHistoryNodeOutputs>());
    }
}
