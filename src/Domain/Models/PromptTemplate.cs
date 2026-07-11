using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManjuCraft.Domain.Models
{
public enum PromptTemplateType
    {
        SystemPrompt,
        RewriteStory,
        ShotAssetExtraction,
        AssetGeneration,
        AssetGenerationActor,
        AssetGenerationScene,
        AssetGenerationProp,
        AssetGenerationBgm,
        AssetGenerationVoiceVoice
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