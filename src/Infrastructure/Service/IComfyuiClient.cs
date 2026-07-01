// @name:         IComfyuiClient
// @author:       AI Assistant
// @namespace:    ManjuCraft.Infrastructure.Service
// @description:  ComfyUI客户端接口
// @version:      1.0
// @date:         2026-06-30

using ManjuCraft.Domain.Models.ComfyUI;

namespace ManjuCraft.Infrastructure.Service
{
    /// <summary>
    /// ComfyUI客户端接口
    /// </summary>
    public interface IComfyuiClient
    {
        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        Task<ComfyuiConnectionStatus> GetConnectionStatusAsync(string apiUrl);

        /// <summary>
        /// 提交提示词
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        /// <param name="workflowJson">工作流JSON</param>
        /// <param name="projectId">项目ID</param>
        Task<Dictionary<string, object>> SubmitPromptAsync(string apiUrl, string workflowJson, long? projectId = null);

        /// <summary>
        /// 获取历史记录
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        /// <param name="promptId">提示词ID</param>
        Task<ComfyuiHistoryItem> GetHistoryAsync(string apiUrl, string promptId);

        /// <summary>
        /// 获取队列信息
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        Task<List<ComfyuiQueueAllResponse>> GetQueueAsync(string apiUrl);

        /// <summary>
        /// 下载输出文件
        /// </summary>
        /// <param name="apiUrl">API地址</param>
        /// <param name="file">文件信息</param>
        /// <param name="localPath">本地路径</param>
        /// <param name="projectId">项目ID</param>
        Task DownloadOutputAsync(string apiUrl, ComfyuiHistoryFile file, string localPath, long projectId);

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
        Task<string> DownloadAndSaveAsync(string apiUrl, string filename, string subpath, string type, string outputDir, string localPath, long projectId);
    }
}
