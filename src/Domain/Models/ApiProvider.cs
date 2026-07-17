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

        /// <summary>人物场景档案</summary>
        TextToImage2 = 6,

        /// <summary>图片到图片</summary>
        ImageToImage = 8,

        /// <summary>图生图 - QWen Image Edit</summary>
        ImageToImageQwen = 9,

        /// <summary>文生音频 - 音乐</summary>
        TextToMusic = 7
    }

    /// <summary>
    /// API 提供者底层类型
    /// </summary>
    public enum ProviderType
    {
        /// <summary>标准 LLM API（OpenAI/Dashscope/Gemini 等兼容协议）</summary>
        LLM = 1,

        /// <summary>ComfyUI 工作流代理</summary>
        ComfyUI = 2
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

        /// <summary>
        /// 底层类型（区分 ComfyUI 工作 Flow 还是标准 LLM API）
        /// </summary>
        [Required]
        public ProviderType Type { get; set; }

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
