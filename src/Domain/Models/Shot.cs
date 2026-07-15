using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
/// <summary>分镜</summary>
public class Shot : BaseEntity
{
    /// <summary>所属剧集ID</summary>
    public long EpisodeId { get; set; }

    /// <summary>分镜编号，如 SH001</summary>
    [Required]
    [StringLength(32)]
    public string ShotNumber { get; set; }

    /// <summary>分镜时长（秒）</summary>
    public float? Duration { get; set; }

    /// <summary>分镜顺序</summary>
    public int Order { get; set; }

    /// <summary>视频资源ID</summary>
    public long? ResourceId { get; set; }

    /// <summary>视频资源</summary>
    public Resource Resource { get; set; }

    /// <summary>所属剧集</summary>
    public Episode Episode { get; set; }

    /// <summary>分镜帧列表</summary>
    public ICollection<ShotFrame> Frames { get; set; }
}
}