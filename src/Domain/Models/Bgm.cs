// @name:         Bgm
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  BGM实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// BGM实体
    /// </summary>
    public class Bgm : BaseEntity
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        [Required]
        public long ProjectId { get; set; }

        /// <summary>
        /// BGM名称
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        /// <summary>
        /// 提示词
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// 默认工作流类型
        /// </summary>
        [Required]
        [StringLength(50)]
        public string DefaultWorkflowType { get; set; } = "MusicGen";

        /// <summary>
        /// 排序
        /// </summary>
        [Required]
        public int Order { get; set; }

        /// <summary>
        /// 所属项目
        /// </summary>
        public Project Project { get; set; }
    }
}
