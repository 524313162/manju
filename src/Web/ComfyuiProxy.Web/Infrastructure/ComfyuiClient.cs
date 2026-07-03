// @name:         ComfyuiClient
// @author:       AI Assistant
// @namespace:    ManjuCraft.Infrastructure.Service
// @description:  ComfyUI客户端实现
// @version:      1.0
// @date:         2026-06-30

using System.Net.Http.Json;
using ManjuCraft.Domain.Models.ComfyUI;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Infrastructure.Service;

/// <summary>
/// ComfyUI客户端实现
/// </summary>
public class ComfyuiClient : IComfyuiClient
{
    private readonly IFileStorageService _fileStorage;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fileStorage">文件存储服务</param>
    public ComfyuiClient(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// 获取连接状态
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    public async Task<ComfyuiConnectionStatus> GetConnectionStatusAsync(string apiUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync(TrailingSlash(apiUrl) + "system_stats");
            if (!response.IsSuccessStatusCode)
            {
                return ComfyuiConnectionStatus.Disconnected($"HTTP {response.StatusCode}");
            }

            var stats = await response.Content.ReadFromJsonAsync<ComfyuiSystemStats>();
            if (stats == null)
                return ComfyuiConnectionStatus.Disconnected("无法解析系统统计信息");

            var vramTotal = ParseBytes(stats.VramTotal);
            var vramFree = ParseBytes(stats.VramFree);

            return ComfyuiConnectionStatus.Connected(new SystemStatsDto
            {
                Device = stats.Device,
                Fp64Enabled = stats.Fp64Enabled == "true",
                VramTotalMB = vramTotal / 1024 / 1024,
                VramFreeMB = vramFree / 1024 / 1024,
                PythonVersion = stats.PythonVersion
            });
        }
        catch (Exception ex)
        {
            return ComfyuiConnectionStatus.Disconnected(ex.Message);
        }
    }

    /// <summary>
    /// 提交提示词
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    /// <param name="workflowJson">工作流JSON</param>
    /// <param name="projectId">项目ID</param>
    public async Task<Dictionary<string, object>> SubmitPromptAsync(string apiUrl, string workflowJson, long? projectId = null)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var content = new StringContent(workflowJson, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(TrailingSlash(apiUrl) + "prompt", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"ComfyUI 提交失败 ({response.StatusCode}): {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return result ?? throw new Exception("ComfyUI 返回空响应");
    }

    /// <summary>
    /// 获取历史记录
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    /// <param name="promptId">提示词ID</param>
    public async Task<ComfyuiHistoryItem> GetHistoryAsync(string apiUrl, string promptId)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await client.GetAsync(TrailingSlash(apiUrl) + "history/" + promptId);

        if (!response.IsSuccessStatusCode)
            return null;

        var history = await response.Content.ReadFromJsonAsync<Dictionary<string, ComfyuiHistoryItem>>();
        return history?.Values.FirstOrDefault();
    }

    /// <summary>
    /// 获取队列信息
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    public async Task<List<ComfyuiQueueAllResponse>> GetQueueAsync(string apiUrl)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await client.GetAsync(TrailingSlash(apiUrl) + "queue");

        if (!response.IsSuccessStatusCode)
            return new List<ComfyuiQueueAllResponse>();

        var data = await response.Content.ReadFromJsonAsync<ComfyuiQueueAllResponse>();
        return data == null ? new List<ComfyuiQueueAllResponse>() : new List<ComfyuiQueueAllResponse> { data };
    }

    /// <summary>
    /// 下载输出文件
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    /// <param name="file">文件信息</param>
    /// <param name="localPath">本地路径</param>
    /// <param name="projectId">项目ID</param>
    public async Task DownloadOutputAsync(string apiUrl, ComfyuiHistoryFile file, string localPath, long projectId)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        var url = $"{TrailingSlash(apiUrl)}view?filename={file.Filename}&subdirectory={file.SubPath}&type={file.Type}";
        var bytes = await client.GetByteArrayAsync(url);
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(localPath, bytes);
    }

    /// <summary>
    /// 下载并保存文件
    /// </summary>
    /// <param name="apiUrl">API地址</param>
    /// <param name="filename">文件名</param>
    /// <param name="subpath">子路径</param>
    /// <param name="type">类型</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="localPath">本地路径</param>
    /// <param name="projectId">项目ID</param>
    public async Task<string> DownloadAndSaveAsync(string apiUrl, string filename, string subpath, string type, string outputDir, string localPath, long projectId)
    {
        try
        {
            await DownloadOutputAsync(apiUrl, new ComfyuiHistoryFile(filename, subpath, type), localPath, projectId);
            return _fileStorage.GetAssetUrl(projectId, Path.GetFileName(Path.GetDirectoryName(localPath)), 0, Path.GetFileNameWithoutExtension(localPath));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ComfyUI] 下载失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析字节字符串
    /// </summary>
    /// <param name="value">字节字符串</param>
    private static long ParseBytes(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var cleaned = value.Trim();
        if (cleaned.Contains("B", StringComparison.OrdinalIgnoreCase))
        {
            var parts = cleaned.Split(' ');
            if (double.TryParse(parts[0], out var num))
            {
                var unit = parts[1].ToUpper();
                if (unit.Contains("TB")) return (long)(num * 1024 * 1024 * 1024 * 1024);
                if (unit.Contains("GB")) return (long)(num * 1024 * 1024 * 1024);
                if (unit.Contains("MB")) return (long)(num * 1024 * 1024);
                if (unit.Contains("KB")) return (long)(num * 1024);
            }
        }
        return long.TryParse(cleaned, out var bytes) ? bytes : 0;
    }

    /// <summary>
    /// 确保URL以斜杠结尾
    /// </summary>
    /// <param name="url">URL字符串</param>
    private static string TrailingSlash(string url)
    {
        return url.EndsWith("/") ? url : url + "/";
    }
}
