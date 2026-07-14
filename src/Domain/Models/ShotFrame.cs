using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class ShotFrame : BaseEntity
    {
        public long ShotId { get; set; }

        public long ProjectId { get; set; }

        [Required]
        [StringLength(32)]
        public string ShotNumber { get; set; }

        [Required]
        [StringLength(32)]
        public string FrameType { get; set; }

        [Required]
        public string Description { get; set; }

        public long? ResourceId { get; set; }

        public float? StartTime { get; set; }

        public float? Duration { get; set; }

        public int Order { get; set; }

        public Shot Shot { get; set; }

        public Project Project { get; set; }

        public Resource Resource { get; set; }

        public ICollection<ShotFrameAsset> ShotFrameAssets { get; set; }
    }
}