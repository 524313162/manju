// @name:         Episode
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  分集实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 分集实体
    /// </summary>
    public class Episode : BaseEntity
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        [Required]
        public long ProjectId { get; set; }

        /// <summary>
        /// 分集名称
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        /// <summary>
        /// 时长（秒）
        /// </summary>
        [Required]
        public int Duration { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        [Required]
        public int Order { get; set; }

        /// <summary>
        /// 所属项目
        /// </summary>
        public Project Project { get; set; }

        /// <summary>
        /// 分镜列表
        /// </summary>
        public ICollection<Shot> Shots { get; set; }
    }
}
