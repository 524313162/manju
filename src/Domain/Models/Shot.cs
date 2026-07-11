using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class Shot : BaseEntity
    {
        public long EpisodeId { get; set; }

        [Required]
        [StringLength(32)]
        public string ShotNumber { get; set; }

        [Required]
        public string Description { get; set; }

        [StringLength(32)]
        public string ShotSize { get; set; }

        [StringLength(64)]
        public string CameraMovement { get; set; }

        public float? Duration { get; set; }

        public int Order { get; set; }

        public Episode Episode { get; set; }

        public ICollection<ShotFrame> Frames { get; set; }

        public ICollection<ShotAsset> ShotAssets { get; set; }
    }
}