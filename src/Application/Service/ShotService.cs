using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    public class ShotService : IShotService
    {
        private readonly IProjectDbContext _db;

        public ShotService(IProjectDbContext db) => _db = db;

        public async Task<List<Shot>> GetByEpisodeAsync(long episodeId)
            => await _db.Shots.Where(s => s.EpisodeId == episodeId).OrderBy(s => s.Order).ToListAsync();

        public async Task<Shot> GetByIdAsync(long id)
            => await _db.Shots.FindAsync(id);

        public async Task<Shot> CreateAsync(Shot shot)
        {
            shot.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            shot.UpdatedTime = shot.CreatedTime;
            await _db.Shots.AddAsync(shot);
            await _db.SaveChangesAsync();
            return shot;
        }

        public async Task<Shot> UpdateAsync(Shot shot)
        {
            var existing = await _db.Shots.FindAsync(shot.Id);
            if (existing == null) return null;
            existing.AssetRefs = shot.AssetRefs;
            existing.Description = shot.Description;
            existing.ShotSize = shot.ShotSize;
            existing.CameraMovement = shot.CameraMovement;
            existing.Duration = shot.Duration;
            existing.Order = shot.Order;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(long id)
        {
            var shot = await _db.Shots.FindAsync(id);
            if (shot != null) { _db.Shots.Remove(shot); await _db.SaveChangesAsync(); }
        }
    }
}