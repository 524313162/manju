using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class ShotFrameAsset : BaseEntity
    {
        public long ShotFrameId { get; set; }

        public Guid AssetId { get; set; }

        [StringLength(64)]
        public string Role { get; set; }

        public int Order { get; set; }

        public ShotFrame ShotFrame { get; set; }

        public Asset Asset { get; set; }
    }
}
