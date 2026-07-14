using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManjuCraft.Domain.Models
{
    public enum PromptTemplateType
    {
        /// <summary>系统提示词</summary>
        SystemPrompt,
        /// <summary>剧本重写提示词</summary>
        RewriteStory,
        /// <summary>分镜与资产提取提示词</summary>
        ShotAssetExtraction,
        /// <summary>一键提取资产信息提示词</summary>
        AssetExtraction,
        /// <summary>分镜头帧资产提取提示词</summary>
        ShotFrameAssetExtraction,
        /// <summary>资产生成提示词（通用）</summary>
        AssetGeneration,
        /// <summary>角色档案生成提示词</summary>
        AssetGenerationActor,
        /// <summary>场景档案生成提示词</summary>
        AssetGenerationScene,
        /// <summary>道具档案生成提示词</summary>
        AssetGenerationProp,
        /// <summary>BGM生成提示词</summary>
        AssetGenerationBgm,
        /// <summary>角色声音生成提示词</summary>
        AssetGenerationVoiceVoice,

        /// <summary>帧图片生成提示词</summary>
        FrameImageGeneration
    }

    public class PromptTemplate : BaseEntity
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        [Required]
        [StringLength(64)]
        public string TemplateType { get; set; }

        [NotMapped]
        public PromptTemplateType TemplateTypeEnum
        {
            get => Enum.TryParse<PromptTemplateType>(TemplateType, true, out var result) ? result : PromptTemplateType.SystemPrompt;
            set => TemplateType = value.ToString();
        }

        [Required]
        public string Content { get; set; }

        public bool IsDefault { get; set; }
    }
}