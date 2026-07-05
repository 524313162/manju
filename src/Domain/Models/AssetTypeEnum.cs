using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public enum AssetTypeEnum
    {
        [Display(Name = "角色")]
        Actor = 1,

        [Display(Name = "场景")]
        Scene = 2,

        [Display(Name = "BGM")]
        Bgm = 3,

        [Display(Name = "技能")]
        Skill = 4,

        [Display(Name = "道具")]
        Prop = 5
    }
}
