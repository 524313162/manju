// @name:         Shot
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  分镜实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 分镜实体
    /// </summary>
    public class Shot : BaseEntity
    {
        /// <summary>
        /// 分集ID
        /// </summary>
        [Required]
        public long EpisodeId { get; set; }

        /// <summary>
        /// 首帧提示词
        /// </summary>
        [Required]
        public string FirstFramePrompt { get; set; }

        /// <summary>
        /// 首帧工作流类型
        /// </summary>
        [Required]
        [StringLength(50)]
        public string FirstFrameWorkflowType { get; set; } = "Img2Img";

        /// <summary>
        /// 台词
        /// </summary>
        public string Dialog { get; set; }

        /// <summary>
        /// 视频提示词
        /// </summary>
        [Required]
        public string VideoPrompt { get; set; }

        /// <summary>
        /// 视频工作流类型
        /// </summary>
        [Required]
        [StringLength(50)]
        public string VideoWorkflowType { get; set; } = "Img2Video";

        /// <summary>
        /// 排序
        /// </summary>
        [Required]
        public int Order { get; set; }

        /// <summary>
        /// 所属分集
        /// </summary>
        public Episode Episode { get; set; }

        /// <summary>
        /// 关联图片集合
        /// </summary>
        public ICollection<EntityImage> Images { get; set; }
    }
}
