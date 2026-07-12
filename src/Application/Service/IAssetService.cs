using ManjuCraft.Application.Service.Dtos;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IAssetService
    {
        Task<List<Asset>> GetByProjectAsync(long projectId, AssetTypeEnum? assetType = null);

        Task<Asset?> GetByIdAsync(Guid id);

        Task<Asset> CreateAsync(CreateAssetDto dto);

        Task<List<Asset>> BulkCreateAsync(BulkCreateDto dto);

        Task<Asset?> UpdateAsync(UpdateAssetDto dto);

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
