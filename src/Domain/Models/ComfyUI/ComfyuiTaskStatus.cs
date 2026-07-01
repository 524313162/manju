// @name:         ComfyuiTaskStatus
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models.ComfyUI
// @description:  ComfyUI任务状态
// @version:      1.0
// @date:         2026-06-30

namespace ManjuCraft.Domain.Models.ComfyUI;

/// <summary>
/// ComfyUI任务状态
/// </summary>
public class ComfyuiTaskStatus
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 任务状态
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 进度
    /// </summary>
    public string Progress { get; set; } = string.Empty;

    /// <summary>
    /// 当前节点
    /// </summary>
    public string Node { get; set; } = string.Empty;

    /// <summary>
    /// 输出路径
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
}
