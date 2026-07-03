using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class Story : BaseEntity
    {
        public long ProjectId { get; set; }

        [Required]
        [StringLength(512)]
        public string Title { get; set; }

        public Project Project { get; set; }

        public ICollection<StoryChapter> Chapters { get; set; }
    }
}