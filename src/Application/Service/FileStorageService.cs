// @name:         FileStorageService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  文件存储服务实现
// @version:      1.0
// @date:         2026-06-30

using Microsoft.AspNetCore.Hosting;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 文件存储服务实现
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="env">Web主机环境</param>
        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// 获取资产URL
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="viewType">视图类型</param>
        public string GetAssetUrl(long projectId, string entityType, long entityId, string viewType)
        {
            return $"/{projectId}/asset/{entityType.ToLower()}/{entityId}/{viewType}";
        }

        /// <summary>
        /// 保存资产文件
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="viewType">视图类型</param>
        /// <param name="data">文件数据</param>
        /// <param name="extension">文件扩展名</param>
        public async Task<string> SaveAssetAsync(long projectId, string entityType, long entityId, string viewType, byte[] data, string extension)
        {
            var contentRoot = _env.ContentRootPath;
            var wwwroot = _env.WebRootPath;
            var dirPath = Path.Combine(wwwroot, $"{projectId}", "asset", entityType.ToLower(), $"{entityId}");
            Directory.CreateDirectory(dirPath);

            var safeExt = extension.StartsWith(".") ? extension : ("." + extension);
            var fileName = viewType + safeExt;
            var filePath = Path.Combine(dirPath, fileName);
            var urlPath = $"{projectId}/asset/{entityType.ToLower()}/{entityId}/{viewType}";

            await File.WriteAllBytesAsync(filePath, data);
            return urlPath;
        }
    }
}
