// @name:         ComfyuiConnectionStatus
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models.ComfyUI
// @description:  ComfyUI连接状态
// @version:      1.0
// @date:         2026-06-30

namespace ManjuCraft.Domain.Models.ComfyUI;

/// <summary>
/// ComfyUI连接状态
/// </summary>
public class ComfyuiConnectionStatus
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 系统统计信息
    /// </summary>
    public SystemStatsDto? SystemStats { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// 创建已连接状态
    /// </summary>
    /// <param name="stats">系统统计信息</param>
    public static ComfyuiConnectionStatus Connected(SystemStatsDto stats)
        => new() { IsConnected = true, SystemStats = stats, LastChecked = DateTime.UtcNow };

    /// <summary>
    /// 创建已断开连接状态
    /// </summary>
    /// <param name="error">错误信息</param>
    public static ComfyuiConnectionStatus Disconnected(string error)
        => new() { IsConnected = false, ErrorMessage = error, LastChecked = DateTime.UtcNow };
}

/// <summary>
/// 系统统计信息
/// </summary>
public class SystemStatsDto
{
    /// <summary>
    /// 设备信息
    /// </summary>
    public string Device { get; set; } = string.Empty;

    /// <summary>
    /// FP64是否启用
    /// </summary>
    public bool Fp64Enabled { get; set; }

    /// <summary>
    /// VRAM总量（MB）
    /// </summary>
    public long VramTotalMB { get; set; }

    /// <summary>
    /// VRAM剩余量（MB）
    /// </summary>
    public long VramFreeMB { get; set; }

    /// <summary>
    /// Python版本
    /// </summary>
    public string PythonVersion { get; set; } = string.Empty;
}
