using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// AI 能力类型
    /// </summary>
    public enum AiCapability
    {
        /// <summary>文本到文本（大语言模型对话）</summary>
        TextToText = 1,

        /// <summary>文本到图片</summary>
        TextToImage = 2,

        /// <summary>文本到音频</summary>
        TextToAudio = 3,

        /// <summary>文本到视频</summary>
        TextToVideo = 4,

        /// <summary>图片到视频</summary>
        ImageToVideo = 5,

        /// <summary>图片编辑（含分镜生成、角色设定等）</summary>
        ImageEdit = 6,

        /// <summary>文生音频 - 音乐</summary>
        TextToMusic = 7,

        /// <summary>本地 ComfyUI - 通用工作流</summary>
        ComfyUI = 100
    }

    /// <summary>
    /// API 提供者 — 纯连接配置
    /// </summary>
    public class ApiProvider : BaseEntity
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        /// <summary>
        /// 支持的 AI 能力（一个 provider 只绑定一个能力，多个能力创建多个记录）
        /// </summary>
        [Required]
        public AiCapability Capability { get; set; }

        [Required]
        [StringLength(512)]
        public string ApiUrl { get; set; }

        [StringLength(1024)]
        public string ApiKey { get; set; }

        /// <summary>
        /// 模型名称（如 deepseek-chat、doubao、qwen 等）
        /// </summary>
        [StringLength(256)]
        public string Model { get; set; }
    }
}
