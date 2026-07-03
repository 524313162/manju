using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    public class StoryService : IStoryService
    {
        private readonly IProjectDbContext _dbContext;

        public StoryService(IProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Story>> GetByProjectIdAsync(long projectId)
            => await _dbContext.Stories
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.CreatedTime)
                .ToListAsync();

        public async Task<Story> GetByIdAsync(long id)
            => await _dbContext.Stories.FindAsync(id);

        public async Task<Story> CreateAsync(Story story)
        {
            story.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            story.UpdatedTime = story.CreatedTime;
            await _dbContext.Stories.AddAsync(story);
            await _dbContext.SaveChangesAsync();
            return story;
        }

        public async Task<Story> UpdateAsync(Story story)
        {
            var existing = await _dbContext.Stories.FindAsync(story.Id);
            if (existing == null) return null;
            existing.Title = story.Title;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(long id)
        {
            var story = await _dbContext.Stories.FindAsync(id);
            if (story != null)
            {
                _dbContext.Stories.Remove(story);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}