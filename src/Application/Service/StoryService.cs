// @name:         StoryService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  故事服务实现
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 故事服务实现
    /// </summary>
    public class StoryService : IStoryService
    {
        private readonly IProjectDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        public StoryService(IProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 根据项目ID获取故事列表
        /// </summary>
        /// <param name="projectId">项目ID</param>
        public async Task<List<Story>> GetByProjectIdAsync(long projectId)
            => await _dbContext.Stories
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.CreatedTime)
                .ToListAsync();

        /// <summary>
        /// 根据ID获取故事
        /// </summary>
        /// <param name="id">故事ID</param>
        public async Task<Story> GetByIdAsync(long id)
            => await _dbContext.Stories.FindAsync(id) ?? new Story();

        /// <summary>
        /// 创建故事
        /// </summary>
        /// <param name="story">故事实体</param>
        public async Task<Story> CreateAsync(Story story)
        {
            story.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            story.UpdatedTime = story.CreatedTime;
            await _dbContext.Stories.AddAsync(story);
            await _dbContext.SaveChangesAsync();
            return story;
        }

        /// <summary>
        /// 更新故事
        /// </summary>
        /// <param name="story">故事实体</param>
        public async Task<Story> UpdateAsync(Story story)
        {
            var existing = await _dbContext.Stories.FindAsync(story.Id);
            if (existing == null) return new Story();
            existing.Content = story.Content;
            existing.SplitContent = story.SplitContent;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// 删除故事
        /// </summary>
        /// <param name="id">故事ID</param>
        public async Task DeleteAsync(long id)
        {
            var story = await _dbContext.Stories.FindAsync(id);
            if (story != null)
            {
                _dbContext.Stories.Remove(story);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 拆分故事内容
        /// </summary>
        /// <param name="id">故事ID</param>
        /// <param name="splitContent">拆分内容</param>
        public async Task<Story> SplitAsync(long id, string splitContent)
        {
            var story = await _dbContext.Stories.FindAsync(id);
            if (story == null) return new Story();
            story.SplitContent = splitContent;
            story.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _dbContext.SaveChangesAsync();
            return story;
        }
    }
}
