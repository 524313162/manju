using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public enum AssetTypeEnum
    {
        [Display(Name = "角色")]
        Actor = 1,

        [Display(Name = "道具")]
        Prop = 5,

        [Display(Name = "场景")]
        Scene = 3,

        [Display(Name = "BGM")]
        Bgm = 4,

        [Display(Name = "声音")]
        VoiceVoice = 2,
    }
}
