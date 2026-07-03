using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    public class ShotFrameService : IShotFrameService
    {
        private readonly IProjectDbContext _db;

        public ShotFrameService(IProjectDbContext db) => _db = db;

        public async Task<List<ShotFrame>> GetByShotAsync(long shotId)
            => await _db.ShotFrames.Where(f => f.ShotId == shotId).OrderBy(f => f.Order).ToListAsync();

        public async Task<ShotFrame> GetByIdAsync(long id)
            => await _db.ShotFrames.FindAsync(id);

        public async Task<ShotFrame> CreateAsync(ShotFrame frame)
        {
            frame.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            frame.UpdatedTime = frame.CreatedTime;
            await _db.ShotFrames.AddAsync(frame);
            await _db.SaveChangesAsync();
            return frame;
        }

        public async Task<ShotFrame> UpdateAsync(ShotFrame frame)
        {
            var existing = await _db.ShotFrames.FindAsync(frame.Id);
            if (existing == null) return null;
            existing.FrameType = frame.FrameType;
            existing.Description = frame.Description;
            existing.ResourceId = frame.ResourceId;
            existing.StartTime = frame.StartTime;
            existing.Duration = frame.Duration;
            existing.Order = frame.Order;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(long id)
        {
            var frame = await _db.ShotFrames.FindAsync(id);
            if (frame != null)
            {
                _db.ShotFrames.Remove(frame);
                await _db.SaveChangesAsync();
            }
        }
    }
}