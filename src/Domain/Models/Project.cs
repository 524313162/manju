using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class Project : BaseEntity
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        public ICollection<Story> Stories { get; set; }

        public ICollection<Asset> Assets { get; set; }

        public ICollection<Episode> Episodes { get; set; }

        public ICollection<ShotFrame> ShotFrames { get; set; }

        public ICollection<Workflow> Workflows { get; set; }
    }
}