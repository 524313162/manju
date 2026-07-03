using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectDbContext _dbContext;

        public ProjectService(IProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Project>> GetAllAsync()
            => await _dbContext.Projects.ToListAsync();

        public async Task<Project> GetByIdAsync(long id)
            => await _dbContext.Projects.FindAsync(id);

        public async Task<Project> CreateAsync(Project project)
        {
            project.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            project.UpdatedTime = project.CreatedTime;
            await _dbContext.Projects.AddAsync(project);
            await _dbContext.SaveChangesAsync();
            return project;
        }

        public async Task<Project> UpdateAsync(Project project)
        {
            var existing = await _dbContext.Projects.FindAsync(project.Id);
            if (existing == null) return null;
            existing.Name = project.Name;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _dbContext.SaveChangesAsync();
            return existing;
        }

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