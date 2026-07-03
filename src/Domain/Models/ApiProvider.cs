using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public class ApiProvider : BaseEntity
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        [Required]
        [StringLength(512)]
        public string ApiUrl { get; set; }

        [StringLength(1024)]
        public string ApiKey { get; set; }

        public string ConfigJson { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }
    }
}