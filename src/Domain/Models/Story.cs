// @name:         Story
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  故事实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 故事实体
    /// </summary>
    public class Story : BaseEntity
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        [Required]
        public long ProjectId { get; set; }

        /// <summary>
        /// 故事内容
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// 拆分内容
        /// </summary>
        public string SplitContent { get; set; } = "";

        /// <summary>
        /// 所属项目
        /// </summary>
        public Project Project { get; set; }

        /// <summary>
        /// 关联演员列表
        /// </summary>
        public ICollection<Actor> Actors { get; set; }
    }
}
