// @name:         IProjectService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  项目管理接口
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 项目服务接口
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// 获取所有项目
        /// </summary>
        Task<List<Project>> GetAllAsync();

        /// <summary>
        /// 根据ID获取项目
        /// </summary>
        /// <param name="id">项目ID</param>
        Task<Project> GetByIdAsync(long id);

        /// <summary>
        /// 创建项目
        /// </summary>
        /// <param name="project">项目实体</param>
        Task<Project> CreateAsync(Project project);

        /// <summary>
        /// 更新项目
        /// </summary>
        /// <param name="project">项目实体</param>
        Task<Project> UpdateAsync(Project project);

        /// <summary>
        /// 删除项目
        /// </summary>
        /// <param name="id">项目ID</param>
        Task DeleteAsync(long id);
    }
}
