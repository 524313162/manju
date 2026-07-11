using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class ShotAsset : BaseEntity
    {
        public long ShotId { get; set; }

        public Guid AssetId { get; set; }

        [StringLength(64)]
        public string Role { get; set; }

        public int Order { get; set; }

        public Shot Shot { get; set; }

        public Asset Asset { get; set; }
    }
}