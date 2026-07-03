using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class PromptTemplate : BaseEntity
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        [Required]
        [StringLength(64)]
        public string TemplateType { get; set; }

        [Required]
        public string Content { get; set; }

        public bool IsDefault { get; set; }
    }
}