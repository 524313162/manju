// @name:         EntityImage
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  实体图片实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 实体图片实体
    /// </summary>
    public class EntityImage : BaseEntity
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        [Required]
        public long ProjectId { get; set; }

        /// <summary>
        /// 实体类型（如Actor、Prop等）
        /// </summary>
        [Required]
        [StringLength(50)]
        public string EntityType { get; set; }

        /// <summary>
        /// 实体ID（关联的实体主键）
        /// </summary>
        [Required]
        public long EntityId { get; set; }

        /// <summary>
        /// 视图类型（如Front、Side等）
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ViewType { get; set; }

        /// <summary>
        /// 媒体类型（Image/Video）
        /// </summary>
        [Required]
        [StringLength(10)]
        public string MediaType { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        [Required]
        [StringLength(1024)]
        public string FilePath { get; set; }

        /// <summary>
        /// 所属项目
        /// </summary>
        public Project Project { get; set; }
    }
}
