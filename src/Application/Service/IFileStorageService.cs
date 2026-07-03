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
        /// <summary>
        /// 保存资产文件
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="viewType">视图类型</param>
        /// <param name="data">文件数据</param>
        /// <param name="extension">文件扩展名</param>
        Task<string> SaveAssetAsync(long projectId, string entityType, long entityId, string viewType, byte[] data, string extension);

        /// <summary>
        /// 获取资产URL
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="viewType">视图类型</param>
        string GetAssetUrl(long projectId, string entityType, long entityId, string viewType);
    }
}
