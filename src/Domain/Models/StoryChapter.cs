using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class StoryChapter : BaseEntity
    {
        public long StoryId { get; set; }

        [Required]
        public int ChapterNumber { get; set; }

        [Required]
        [StringLength(256)]
        public string ChapterName { get; set; }

        [Required]
        public string Content { get; set; }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string Assets { get; set; }

        public int SortOrder { get; set; }

        public Story Story { get; set; }
    }
}