using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IAssetService
    {
        Task<List<Asset>> GetByProjectAsync(long projectId, AssetTypeEnum? assetType = null);

        Task<Asset?> GetByIdAsync(Guid id);

        Task<Asset> CreateAsync(Asset asset);

        Task<List<Asset>> BulkCreateAsync(List<Asset> assets);

        Task<Asset?> UpdateAsync(Asset asset);

        Task DeleteAsync(Guid id);

        Task ReorderAsync(List<ReorderItem> items);

        Task<List<Asset>> GetVariantsAsync(Guid parentId);
    }

    public class ReorderItem
    {
        public Guid Id { get; set; }
        public int Order { get; set; }
    }
}
