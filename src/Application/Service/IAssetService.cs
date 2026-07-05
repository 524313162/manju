using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IAssetService
    {
        Task<List<Asset>> GetByProjectAsync(long projectId, AssetTypeEnum? assetType = null);

        Task<Asset> GetByIdAsync(long id);

        Task<Asset> CreateAsync(Asset asset);

        Task<Asset> UpdateAsync(Asset asset);

        Task DeleteAsync(long id);

        Task ReorderAsync(List<ReorderItem> items);

        Task<List<Asset>> GetVariantsAsync(long parentId);
    }

    public class ReorderItem
    {
        public long Id { get; set; }
        public int Order { get; set; }
    }
}
