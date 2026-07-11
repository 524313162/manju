// @name:         IFileStorageService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  文件存储服务接口
// @version:      1.0
// @date:         2026-06-30

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 文件存储服务接口
    /// </summary>
    public interface IFileStorageService
    {
        Task<string> SaveAssetAsync(long projectId, string entityType, Guid entityId, byte[] data, string extension);

        string GetAssetUrl(long projectId, string entityType, Guid entityId, string extension);
    }
}
