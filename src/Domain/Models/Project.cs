// @name:         Project
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  项目实体
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 项目实体
    /// </summary>
    public class Project : BaseEntity
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        /// <summary>
        /// ComfyUI配置JSON
        /// </summary>
        public string ComfyuiConfigJson { get; set; } = "";

        /// <summary>
        /// 故事列表
        /// </summary>
        public ICollection<Story> Stories { get; set; }

        /// <summary>
        /// 演员列表
        /// </summary>
        public ICollection<Actor> Actors { get; set; }

        /// <summary>
        /// 道具列表
        /// </summary>
        public ICollection<Prop> Props { get; set; }

        /// <summary>
        /// 场景列表
        /// </summary>
        public ICollection<Scene> Scenes { get; set; }

        /// <summary>
        /// 技能列表
        /// </summary>
        public ICollection<Skill> Skills { get; set; }

        /// <summary>
        /// BGM列表
        /// </summary>
        public ICollection<Bgm> Bgms { get; set; }

        /// <summary>
        /// 分集列表
        /// </summary>
        public ICollection<Episode> Episodes { get; set; }

        /// <summary>
        /// 实体图片列表
        /// </summary>
        public ICollection<EntityImage> EntityImages { get; set; }

        /// <summary>
        /// 工作流列表
        /// </summary>
        public ICollection<Workflow> Workflows { get; set; }

        /// <summary>
        /// 获取ComfyUI配置字典
        /// </summary>
        public Dictionary<string, string> GetComfyuiConfig()
        {
            if (string.IsNullOrWhiteSpace(ComfyuiConfigJson))
                return new Dictionary<string, string>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(ComfyuiConfigJson)
                    ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
    }
}
