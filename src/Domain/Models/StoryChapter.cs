using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class StoryChapter : BaseEntity
    {
        public long StoryId { get; set; }

        [Required]
        [StringLength(32)]
        public string ChapterNumber { get; set; }

        [Required]
        [StringLength(256)]
        public string ChapterName { get; set; }

        [Required]
        public string Content { get; set; }

        public int Order { get; set; }

        public Story Story { get; set; }
    }
}