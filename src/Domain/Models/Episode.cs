using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class Episode : BaseEntity
    {
        public long ProjectId { get; set; }

        public long? StoryChapterId { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        public int Duration { get; set; }

        public int Order { get; set; }

        public Project Project { get; set; }

        public StoryChapter StoryChapter { get; set; }

        public ICollection<Shot> Shots { get; set; }
    }
}