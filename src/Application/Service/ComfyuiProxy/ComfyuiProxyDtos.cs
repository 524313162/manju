using System.Text.Json.Nodes;

namespace ManjuCraft.Application.Service.ComfyuiProxy;

/// <summary>
/// ComfyUI 代理提交响应（从 ComfyuiProxy.Web 返回）
/// </summary>
public class ComfyuiSubmitResponseDto
{
    public string PromptId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
}

/// <summary>
/// ComfyUI 代理结果查询响应
/// </summary>
public class ComfyuiResultResponseDto
{
    public bool Success { get; set; }
    public string PromptId { get; set; } = string.Empty;
    public ComfyuiOutputs? Outputs { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 通用输出封装
/// </summary>
public class ComfyuiOutputs
{
    public List<ComfyuiImageOutput>? ImageUrls { get; set; }
    public List<ComfyuiVideoOutput>? VideoUrls { get; set; }
    public List<ComfyuiAudioOutput>? AudioUrls { get; set; }
    public string? Text { get; set; }
}

/// <summary>
/// ComfyUI 图片输出项
/// </summary>
public class ComfyuiImageOutput
{
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Subfolder { get; set; }
}

/// <summary>
/// ComfyUI 视频输出项
/// </summary>
public class ComfyuiVideoOutput
{
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Subfolder { get; set; }
}

/// <summary>
/// ComfyUI 音频输出项
/// </summary>
public class ComfyuiAudioOutput
{
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Subfolder { get; set; }
}

// ─── 工作流特定输出类型 ───

/// <summary>
/// 图片列表输出（用于 GenImage / GenStoryboard / GenFrame）
/// </summary>
public class ComfyuiImageListOutput
{
    public List<string> Urls { get; set; } = new();
}

/// <summary>
/// 视频列表输出（用于 GenTextToVideo / GenImageToVideo）
/// </summary>
public class ComfyuiVideoListOutput
{
    public List<string> Urls { get; set; } = new();
}

/// <summary>
/// 音频列表输出（用于 GenBgm / GenAceMusic）
/// </summary>
public class ComfyuiAudioListOutput
{
    public List<string> Urls { get; set; } = new();
}

/// <summary>
/// 文本输出（用于 Chat / StoryGeneration / StoryRewrite）
/// </summary>
public class ComfyuiTextOutput
{
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 分镜图输出
/// </summary>
public class ComfyuiStoryboardOutput
{
    public List<string> ImageUrls { get; set; } = new();
}

/// <summary>
/// 人物档案输出
/// </summary>
public class ComfyuiCharProfileOutput
{
    public List<string> ImageUrls { get; set; } = new();
}

// ─── 提交请求 DTO ───

public class ComfyuiWorkflowRequestDto
{
    public string WorkflowType { get; set; } = string.Empty;
    public JsonObject Parameters { get; set; } = new();
}
