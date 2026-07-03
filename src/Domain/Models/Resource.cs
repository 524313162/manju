using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class Resource : BaseEntity
    {
        [Required]
        [StringLength(16)]
        public string MediaType { get; set; }

        [Required]
        [StringLength(1024)]
        public string FilePath { get; set; }
    }
}