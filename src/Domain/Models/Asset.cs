using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class Asset : BaseEntity
    {
        public long ProjectId { get; set; }

        public long? ResourceId { get; set; }

        [Required]
        [StringLength(32)]
        public string AssetType { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        public string Description { get; set; }

        public long? ParentId { get; set; }

        public int Order { get; set; }

        public Project Project { get; set; }

        public Resource Resource { get; set; }

        public Asset Parent { get; set; }

        public ICollection<Asset> Children { get; set; }
    }
}