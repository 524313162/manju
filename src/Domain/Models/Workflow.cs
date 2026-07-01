// @name:         Workflow
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  工作流实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 工作流实体
    /// </summary>
    public class Workflow : BaseEntity
    {
        /// <summary>
        /// 项目ID（可为空表示全局工作流）
        /// </summary>
        public long? ProjectId { get; set; }

        /// <summary>
        /// 工作流名称
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        /// <summary>
        /// 工作流类型（如Txt2Img、Img2Img等）
        /// </summary>
        [Required]
        [StringLength(50)]
        public string WorkflowType { get; set; }

        /// <summary>
        /// 配置JSON
        /// </summary>
        [Required]
        public string ConfigJson { get; set; }

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
