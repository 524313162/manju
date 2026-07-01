// @name:         IComfyuiConnectionService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Infrastructure.Service
// @description:  ComfyUI连接服务接口
// @version:      1.0
// @date:         2026-06-30

using ManjuCraft.Domain.Models.ComfyUI;

namespace ManjuCraft.Infrastructure.Service
{
    /// <summary>
    /// ComfyUI连接服务接口
    /// </summary>
    public interface IComfyuiConnectionService
    {
        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        /// <param name="ct">取消令牌</param>
        Task<ComfyuiConnectionStatus> TestConnectionAsync(string apiUrl, CancellationToken ct = default);

        /// <summary>
        /// 获取系统统计
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        /// <param name="ct">取消令牌</param>
        Task<ComfyuiConnectionStatus> GetSystemStatsAsync(string apiUrl, CancellationToken ct = default);
    }

    /// <summary>
    /// ComfyUI连接服务实现
    /// </summary>
    public class ComfyuiConnectionService : IComfyuiConnectionService
    {
        private readonly IComfyuiClient _client;
        private readonly Dictionary<string, ComfyuiConnectionStatus> _cache = new();
        private readonly object _lock = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="client">ComfyUI客户端</param>
        public ComfyuiConnectionService(IComfyuiClient client)
        {
            _client = client;
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        /// <param name="ct">取消令牌</param>
        public async Task<ComfyuiConnectionStatus> TestConnectionAsync(string apiUrl, CancellationToken ct = default)
        {
            var status = await _client.GetConnectionStatusAsync(apiUrl);
            lock (_lock) _cache[apiUrl] = status;
            return status;
        }

        /// <summary>
        /// 获取系统统计
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        /// <param name="ct">取消令牌</param>
        public async Task<ComfyuiConnectionStatus> GetSystemStatsAsync(string apiUrl, CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(apiUrl, out var cached) &&
                    (DateTime.UtcNow - cached.LastChecked) < _cacheExpiration && cached.IsConnected)
                {
                    return cached;
                }
            }

            var status = await _client.GetConnectionStatusAsync(apiUrl);
            lock (_lock) _cache[apiUrl] = status;
            return status;
        }
    }
}
