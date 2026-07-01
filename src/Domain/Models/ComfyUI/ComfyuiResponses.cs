// @name:         ComfyuiResponses
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models.ComfyUI
// @description:  ComfyUI响应数据模型
// @version:      1.0
// @date:         2026-06-30

namespace ManjuCraft.Domain.Models.ComfyUI;

/// <summary>
/// ComfyUI工作流响应
/// </summary>
/// <param name="NodeDescriptions">节点描述映射</param>
public record ComfyuiWorkflowsResponse(
    Dictionary<string, List<ComfyuiWorkflowMeta>> NodeDescriptions
);

/// <summary>
/// ComfyUI工作流元数据
/// </summary>
/// <param name="Name">名称</param>
/// <param name="Input">输入描述</param>
public record ComfyuiWorkflowMeta(
    string Name,
    string Input
);

/// <summary>
/// ComfyUI队列响应
/// </summary>
/// <param name="Number">当前队列号</param>
/// <param name="QueueRemaining">剩余队列数</param>
public record ComfyuiQueueResponse(
    int Number,
    int QueueRemaining
);

/// <summary>
/// ComfyUI全部队列响应
/// </summary>
/// <param name="Pending">等待中项目</param>
/// <param name="Running">运行中项目</param>
public record ComfyuiQueueAllResponse(
    List<ComfyuiPendingItem> Pending,
    List<ComfyuiRunningItem> Running
);

/// <summary>
/// ComfyUI等待中项目
/// </summary>
/// <param name="PipelineItemId">流水线项目ID</param>
/// <param name="PromptId">提示词ID</param>
/// <param name="Prompt">提示词信息</param>
public record ComfyuiPendingItem(
    string PipelineItemId,
    string PromptId,
    ComfyuiPrompt Prompt
);

/// <summary>
/// ComfyUI运行中项目
/// </summary>
/// <param name="PipelineItemId">流水线项目ID</param>
/// <param name="PromptId">提示词ID</param>
/// <param name="Prompt">提示词信息</param>
public record ComfyuiRunningItem(
    string PipelineItemId,
    string PromptId,
    ComfyuiPrompt Prompt
);

/// <summary>
/// ComfyUI提示词
/// </summary>
/// <param name="Nodes">节点映射</param>
/// <param name="Inputs">输入参数</param>
public record ComfyuiPrompt(
    Dictionary<string, ComfyuiPromptNode> Nodes,
    Dictionary<string, object> Inputs
);

/// <summary>
/// ComfyUI提示词节点
/// </summary>
/// <param name="ClassType">类类型</param>
/// <param name="Inputs">输入</param>
/// <param name="Outputs">输出</param>
/// <param name="OverwriteDisplayNode">覆盖显示节点</param>
public record ComfyuiPromptNode(
    object ClassType,
    Dictionary<string, object> Inputs,
    object Outputs,
    object OverwriteDisplayNode
);

/// <summary>
/// ComfyUI历史项
/// </summary>
/// <param name="Outputs">节点输出映射</param>
public record ComfyuiHistoryItem(
    Dictionary<string, ComfyuiHistoryNodeOutputs> Outputs
);

/// <summary>
/// ComfyUI历史节点输出
/// </summary>
/// <param name="Images">图片列表映射</param>
public record ComfyuiHistoryNodeOutputs(
    Dictionary<string, List<ComfyuiHistoryFile>> Images
);

/// <summary>
/// ComfyUI历史文件
/// </summary>
/// <param name="Filename">文件名</param>
/// <param name="SubPath">子路径</param>
/// <param name="Type">类型</param>
public record ComfyuiHistoryFile(
    string Filename,
    string SubPath,
    string Type
);

/// <summary>
/// ComfyUI系统统计
/// </summary>
/// <param name="Device">设备信息</param>
/// <param name="MainThreadAvailable">主线程可用</param>
/// <param name="PythonVersion">Python版本</param>
/// <param name="Fp64Enabled">FP64启用</param>
/// <param name="VramTotal">VRAM总量</param>
/// <param name="VramFree">VRAM剩余</param>
/// <param name="TorchCudaVersion">Torch CUDA版本</param>
public record ComfyuiSystemStats(
    string Device,
    string MainThreadAvailable,
    string PythonVersion,
    string Fp64Enabled,
    string VramTotal,
    string VramFree,
    string TorchCudaVersion
);
