using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service.Dtos;
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

        public async Task<Asset> CreateAsync(CreateAssetDto dto)
        {
            var asset = MapToAsset(dto);
            asset.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            asset.UpdatedTime = asset.CreatedTime;
            await _db.Assets.AddAsync(asset);
            await _db.SaveChangesAsync();
            return asset;
        }

        public async Task<List<Asset>> BulkCreateAsync(BulkCreateDto dto)
        {
            var existingAssets = await GetByProjectAsync(dto.ProjectId);
            var existingByName = existingAssets.ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<Asset>();
            var toUpdate = new List<Asset>();
            var nameToAsset = new Dictionary<string, Asset>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in dto.Assets)
            {
                if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.AssetType))
                    continue;

                var name = item.Name.Trim();
                var type = ParseAssetType(item.AssetType.Trim());

                if (existingByName.TryGetValue(name, out var existing))
                {
                    if (item.Override)
                    {
                        existing.Description = item.Description?.Trim() ?? existing.Description;
                        existing.AssetType = type;
                        toUpdate.Add(existing);
                    }
                    nameToAsset[name] = existing;
                    continue;
                }

                var maxOrder = 0;
                var last = (await GetByProjectAsync(dto.ProjectId, type)).LastOrDefault();
                if (last != null) maxOrder = last.Order;
                var sameType = toAdd.Where(a => a.AssetType == type).ToList();
                if (sameType.Count > 0) maxOrder = Math.Max(maxOrder, sameType.Max(a => a.Order));

                var asset = new Asset
                {
                    ProjectId = dto.ProjectId,
                    AssetType = type,
                    Name = name,
                    Description = item.Description?.Trim() ?? "",
                    Order = maxOrder + 1
                };

                toAdd.Add(asset);
                nameToAsset[name] = asset;
            }

            foreach (var asset in toAdd)
            {
                if (asset.Id == Guid.Empty)
                    asset.Id = Guid.NewGuid();
            }

            foreach (var item in dto.Assets)
            {
                var parentName = item.ParentName?.Trim();
                if (string.IsNullOrEmpty(parentName)) continue;
                if (!nameToAsset.TryGetValue(item.Name.Trim(), out var child)) continue;
                if (nameToAsset.TryGetValue(parentName, out var parent))
                    child.ParentId = parent.Id;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (toAdd.Count > 0)
            {
                foreach (var asset in toAdd)
                {
                    if (asset.Id == Guid.Empty) asset.Id = Guid.NewGuid();
                    asset.CreatedTime = now;
                    asset.UpdatedTime = now;
                }
                await _db.Assets.AddRangeAsync(toAdd);
            }

            foreach (var asset in toUpdate)
            {
                asset.UpdatedTime = now;
            }

            await _db.SaveChangesAsync();

            var result = new List<Asset>(toAdd.Count + toUpdate.Count);
            result.AddRange(toAdd);
            result.AddRange(toUpdate);
            return result;
        }

        public async Task<Asset?> UpdateAsync(UpdateAssetDto dto)
        {
            if (!Guid.TryParse(dto.Id, out var id)) return null;

            var existing = await _db.Assets.FindAsync(id);
            if (existing == null) return null;

            existing.Name = dto.Name;
            existing.Description = dto.Description ?? "";
            if (dto.Order.HasValue)
                existing.Order = dto.Order.Value;

            if (!string.IsNullOrEmpty(dto.AssetType))
                existing.AssetType = ParseAssetType(dto.AssetType);

            if (!string.IsNullOrEmpty(dto.ParentId) && Guid.TryParse(dto.ParentId, out var pid))
                existing.ParentId = pid;

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

        private static Asset MapToAsset(CreateAssetDto dto)
        {
            var type = ParseAssetType(dto.AssetType);
            Guid? parentId = null;
            if (!string.IsNullOrEmpty(dto.ParentId) && Guid.TryParse(dto.ParentId, out var pid))
                parentId = pid;

            return new Asset
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                AssetType = type,
                Name = dto.Name,
                Description = dto.Description ?? "",
                ParentId = parentId,
                Order = dto.Order
            };
        }

        private static AssetTypeEnum ParseAssetType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr)) return AssetTypeEnum.Actor;

            if (int.TryParse(typeStr, out var intVal))
            {
                if (Enum.IsDefined(typeof(AssetTypeEnum), intVal))
                    return (AssetTypeEnum)intVal;
            }

            return typeStr switch
            {
                "Actor" => AssetTypeEnum.Actor,
                "角色" => AssetTypeEnum.Actor,
                "Scene" => AssetTypeEnum.Scene,
                "场景" => AssetTypeEnum.Scene,
                "Bgm" or "BGM" => AssetTypeEnum.Bgm,
                "Prop" => AssetTypeEnum.Prop,
                "道具" => AssetTypeEnum.Prop,
                "VoiceVoice" => AssetTypeEnum.VoiceVoice,
                "声音" => AssetTypeEnum.VoiceVoice,
                "Voice" => AssetTypeEnum.VoiceVoice,
                _ => AssetTypeEnum.Actor
            };
        }
    }
}