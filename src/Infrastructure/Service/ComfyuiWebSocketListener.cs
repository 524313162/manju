// @name:         ComfyuiWebSocketListener
// @author:       AI Assistant
// @namespace:    ManjuCraft.Infrastructure.Service
// @description:  ComfyUI WebSocket监听器
// @version:      1.0
// @date:         2026-06-30

using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ManjuCraft.Infrastructure.Service;

/// <summary>
/// ComfyUI WebSocket监听器接口
/// </summary>
public interface IComfyuiWebSocketListener : IDisposable
{
    /// <summary>
    /// 队列进度事件
    /// </summary>
    event Action<string, string> OnQueueProgress;

    /// <summary>
    /// 完成事件
    /// </summary>
    event Action<string, string> OnCompleted;

    /// <summary>
    /// 错误事件
    /// </summary>
    event Action<string, string> OnError;

    /// <summary>
    /// 开始监听
    /// </summary>
    /// <param name="wsUrl">WebSocket地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartAsync(string wsUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交提示词
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    /// <param name="workflowJson">工作流JSON</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SubmitPromptAsync(string apiUrl, string workflowJson, CancellationToken cancellationToken = default);
}

/// <summary>
/// ComfyUI WebSocket监听器实现
/// </summary>
public class ComfyuiWebSocketListener : IComfyuiWebSocketListener
{
    private bool _disposed = false;

    /// <summary>
    /// 队列进度事件
    /// </summary>
    public event Action<string, string> OnQueueProgress = delegate { };

    /// <summary>
    /// 完成事件
    /// </summary>
    public event Action<string, string> OnCompleted = delegate { };

    /// <summary>
    /// 错误事件
    /// </summary>
    public event Action<string, string> OnError = delegate { };

    /// <summary>
    /// 开始监听
    /// </summary>
    /// <param name="wsUrl">WebSocket地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartAsync(string wsUrl, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        while (!cts.Token.IsCancellationRequested && !_disposed)
        {
            try
            {
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(wsUrl), cts.Token);
                await ReceiveLoop(ws, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OnError("websocket", ex.Message);
                if (!cts.Token.IsCancellationRequested)
                    await Task.Delay(3000, cts.Token);
            }
        }
    }

    /// <summary>
    /// 接收循环
    /// </summary>
    /// <param name="ws">WebSocket实例</param>
    /// <param name="token">取消令牌</param>
    private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken token)
    {
        var buffer = new byte[1024 * 1024];
        while (!ws.CloseStatus.HasValue && !_disposed)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            if (result.MessageType == WebSocketMessageType.Close) break;
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ProcessMessage(json);
        }
    }

    /// <summary>
    /// 处理WebSocket消息
    /// </summary>
    /// <param name="json">JSON消息</param>
    private void ProcessMessage(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp)) return;

            var type = typeProp.GetString() ?? "";
            if (type == "progress")
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("val", out var val) &&
                    data.TryGetProperty("max", out var max))
                {
                    var pct = $"{(int)(val.GetInt32() / (double)max.GetInt32() * 100):F0}%";
                    OnQueueProgress("progress", pct);
                }
            }
            else if (type == "executing")
            {
                var data = root.GetProperty("data");
                var nodeId = data.ValueKind == JsonValueKind.String ? data.GetString() : null;
                if (string.IsNullOrEmpty(nodeId))
                {
                    OnCompleted("complete", root.TryGetProperty("data", out _) ? "done" : "");
                }
                else
                {
                    OnQueueProgress("executing", nodeId);
                }
            }
            else if (type == "execution_error")
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("error", out var err))
                {
                    OnError("error", err.ToString());
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 提交提示词
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    /// <param name="workflowJson">工作流JSON</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SubmitPromptAsync(string apiUrl, string workflowJson, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var content = new StringContent(workflowJson, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(TrailingSlash(apiUrl) + "/prompt", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"ComfyUI 提交失败: {error}");
        }
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        if (result?.TryGetValue("prompt_id", out var promptId) == true)
        {
            OnCompleted("prompt_submitted", promptId.ToString());
        }
    }

    /// <summary>
    /// 确保URL以斜杠结尾
    /// </summary>
    /// <param name="url">URL字符串</param>
    private static string TrailingSlash(string url)
    {
        return url.EndsWith("/") ? url : url + "/";
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }
}
