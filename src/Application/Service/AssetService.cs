using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    public class AssetService : IAssetService
    {
        private readonly IProjectDbContext _db;

        public AssetService(IProjectDbContext db) => _db = db;

        public async Task<List<Asset>> GetByProjectAsync(long projectId, AssetTypeEnum? assetType = null)
        {
            var query = _db.Assets.Where(a => a.ProjectId == projectId).AsQueryable();
            if (assetType.HasValue)
                query = query.Where(a => a.AssetType == assetType.Value);
            return await query.Include(a => a.Resource).OrderBy(a => a.Order).ToListAsync();
        }

        public async Task<Asset?> GetByIdAsync(Guid id)
            => await _db.Assets.FindAsync(id);

        public async Task<Asset> CreateAsync(Asset asset)
        {
            if (asset.Id == Guid.Empty)
                asset.Id = Guid.NewGuid();
            asset.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            asset.UpdatedTime = asset.CreatedTime;
            await _db.Assets.AddAsync(asset);
            await _db.SaveChangesAsync();
            return asset;
        }

        public async Task<List<Asset>> BulkCreateAsync(List<Asset> assets)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var asset in assets)
            {
                if (asset.Id == Guid.Empty)
                    asset.Id = Guid.NewGuid();
                asset.CreatedTime = now;
                asset.UpdatedTime = now;
            }
            await _db.Assets.AddRangeAsync(assets);
            await _db.SaveChangesAsync();
            return assets;
        }

        public async Task<Asset?> UpdateAsync(Asset asset)
        {
            var existing = await _db.Assets.FindAsync(asset.Id);
            if (existing == null) return null;
            existing.Name = asset.Name;
            existing.AssetType = asset.AssetType;
            existing.Description = asset.Description;
            existing.ResourceId = asset.ResourceId;
            existing.ParentId = asset.ParentId;
            existing.Order = asset.Order;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(Guid id)
        {
            var asset = await _db.Assets.FindAsync(id);
            if (asset != null)
            {
                _db.Assets.Remove(asset);
                await _db.SaveChangesAsync();
            }
        }

        public async Task ReorderAsync(List<ReorderItem> items)
        {
            foreach (var item in items)
            {
                var asset = await _db.Assets.FindAsync(item.Id);
                if (asset != null)
                {
                    asset.Order = item.Order;
                    asset.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
            }
            await _db.SaveChangesAsync();
        }

        public async Task<List<Asset>> GetVariantsAsync(Guid parentId)
            => await _db.Assets.Where(a => a.ParentId == parentId).OrderBy(a => a.Order).ToListAsync();
    }
}