// @name:         ProjectService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  项目管理实现
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 项目服务实现
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly IProjectDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        public ProjectService(IProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 获取所有项目
        /// </summary>
        public async Task<List<Project>> GetAllAsync()
            => await _dbContext.Projects.ToListAsync();

        /// <summary>
        /// 根据ID获取项目
        /// </summary>
        /// <param name="id">项目ID</param>
        public async Task<Project> GetByIdAsync(long id)
            => await _dbContext.Projects.FindAsync(id) ?? new Project();

        /// <summary>
        /// 创建项目
        /// </summary>
        /// <param name="project">项目实体</param>
        public async Task<Project> CreateAsync(Project project)
        {
            project.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            project.UpdatedTime = project.CreatedTime;
            await _dbContext.Projects.AddAsync(project);
            await _dbContext.SaveChangesAsync();
            return project;
        }

        /// <summary>
        /// 更新项目
        /// </summary>
        /// <param name="project">项目实体</param>
        public async Task<Project> UpdateAsync(Project project)
        {
            var existing = await _dbContext.Projects.FindAsync(project.Id);
            if (existing == null) return new Project();
            existing.Name = project.Name;
            existing.ComfyuiConfigJson = project.ComfyuiConfigJson;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        /// <param name="id">项目ID</param>
        public async Task DeleteAsync(long id)
        {
            var project = await _dbContext.Projects.FindAsync(id);
            if (project != null)
            {
                _dbContext.Projects.Remove(project);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
