// @name:         IStoryService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  故事服务接口
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 故事服务接口
    /// </summary>
    public interface IStoryService
    {
        /// <summary>
        /// 根据项目ID获取故事列表
        /// </summary>
        /// <param name="projectId">项目ID</param>
        Task<List<Story>> GetByProjectIdAsync(long projectId);

        /// <summary>
        /// 根据ID获取故事
        /// </summary>
        /// <param name="id">故事ID</param>
        Task<Story> GetByIdAsync(long id);

        /// <summary>
        /// 创建故事
        /// </summary>
        /// <param name="story">故事实体</param>
        Task<Story> CreateAsync(Story story);

        /// <summary>
        /// 更新故事
        /// </summary>
        /// <param name="story">故事实体</param>
        Task<Story> UpdateAsync(Story story);

        /// <summary>
        /// 删除故事
        /// </summary>
        /// <param name="id">故事ID</param>
        Task DeleteAsync(long id);

        /// <summary>
        /// 拆分故事
        /// </summary>
        /// <param name="id">故事ID</param>
        /// <param name="splitContent">拆分内容</param>
        Task<Story> SplitAsync(long id, string splitContent);
    }
}
