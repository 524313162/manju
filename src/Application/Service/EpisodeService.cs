using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    public class EpisodeService : IEpisodeService
    {
        private readonly IProjectDbContext _db;

        public EpisodeService(IProjectDbContext db) => _db = db;

        public async Task<List<Episode>> GetByProjectAsync(long projectId)
            => await _db.Episodes.Where(e => e.ProjectId == projectId).OrderBy(e => e.Order).ToListAsync();

        public async Task<Episode> GetByIdAsync(long id)
            => await _db.Episodes.FindAsync(id);

        public async Task<Episode> CreateAsync(Episode ep)
        {
            ep.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ep.UpdatedTime = ep.CreatedTime;
            await _db.Episodes.AddAsync(ep);
            await _db.SaveChangesAsync();
            return ep;
        }

        public async Task<Episode> UpdateAsync(Episode ep)
        {
            var existing = await _db.Episodes.FindAsync(ep.Id);
            if (existing == null) return null;
            existing.Name = ep.Name;
            existing.Duration = ep.Duration;
            existing.StoryChapterId = ep.StoryChapterId;
            existing.Order = ep.Order;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(long id)
        {
            var ep = await _db.Episodes.FindAsync(id);
            if (ep != null)
            {
                _db.Episodes.Remove(ep);
                await _db.SaveChangesAsync();
            }
        }

        public async Task UpdateOrderAsync(long id, int newOrder)
        {
            var ep = await _db.Episodes.FindAsync(id);
            if (ep != null)
            {
                ep.Order = newOrder;
                ep.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await _db.SaveChangesAsync();
            }
        }
    }
}