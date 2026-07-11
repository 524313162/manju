using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class Asset : BaseEntity<Guid>
    {
        public long ProjectId { get; set; }

        public long? ResourceId { get; set; }

        [Required]
        public AssetTypeEnum AssetType { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;

        public Guid? ParentId { get; set; }

        public int Order { get; set; }

        public Project Project { get; set; }

        public Resource Resource { get; set; }

        public Asset Parent { get; set; }

        public ICollection<Asset> Children { get; set; }
    }
}
