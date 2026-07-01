// @name:         Actor
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  演员实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 演员实体
    /// </summary>
    public class Actor : BaseEntity
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        [Required]
        public long ProjectId { get; set; }

        /// <summary>
        /// 演员名称
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 四视图提示词
        /// </summary>
        public string FourViewPrompt { get; set; }

        /// <summary>
        /// 默认工作流类型
        /// </summary>
        [Required]
        [StringLength(50)]
        public string DefaultWorkflowType { get; set; } = "Txt2Img";

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
        /// 关联图片集合
        /// </summary>
        public ICollection<EntityImage> Images { get; set; }
    }
}
