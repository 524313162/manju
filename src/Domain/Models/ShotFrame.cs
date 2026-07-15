using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
/// <summary>分镜帧</summary>
public class ShotFrame : BaseEntity
{
    /// <summary>所属分镜ID</summary>
    public long ShotId { get; set; }

    /// <summary>所属项目ID</summary>
    public long ProjectId { get; set; }

    /// <summary>分镜编号，与所属分镜一致</summary>
    [Required]
    [StringLength(32)]
    public string ShotNumber { get; set; }

    /// <summary>帧类型：First=首帧、Middle=中间帧、Last=末帧</summary>
    [Required]
    [StringLength(32)]
    public string FrameType { get; set; }

    /// <summary>叙事描述，用于视频生成拼接</summary>
    [Required]
    public string NarrativeDescription { get; set; }

    /// <summary>图生图提示词，含构图+角色+场景+光影+镜头</summary>
    public string GeneratePrompt { get; set; }

    /// <summary>镜头运动：固定/前推/拉远/平移等</summary>
    [StringLength(64)]
    public string CameraMovement { get; set; }

    /// <summary>景别：全景/中景/特写/远景/近景</summary>
    [StringLength(32)]
    public string ShotSize { get; set; }

    /// <summary>帧图资源ID</summary>
    public long? ResourceId { get; set; }

    /// <summary>帧内起始时间（秒）</summary>
    public float? StartTime { get; set; }

    /// <summary>帧时长（秒）</summary>
    public float? Duration { get; set; }

    /// <summary>帧顺序</summary>
    public int Order { get; set; }

    /// <summary>所属分镜</summary>
    public Shot Shot { get; set; }

    /// <summary>所属项目</summary>
    public Project Project { get; set; }

    /// <summary>帧图资源</summary>
    public Resource Resource { get; set; }

    /// <summary>绑定到该帧的资产列表</summary>
    public ICollection<ShotFrameAsset> ShotFrameAssets { get; set; }
}
}