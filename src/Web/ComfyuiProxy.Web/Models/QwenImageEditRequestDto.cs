namespace ComfyuiProxy.Web.Models;

/// <summary>
/// QWen 图生图 (Image Edit 2509) 工作流请求参数
/// </summary>
public class QwenImageEditRequestDto
{
    /// <summary>主输入图片（已上传到 ComfyUI 的文件名）</summary>
    public string ImagePath1 { get; set; } = string.Empty;

    /// <summary>第二张参考图片（已上传到 ComfyUI 的文件名，可留空）</summary>
    public string ImagePath2 { get; set; } = string.Empty;

    /// <summary>第三张参考图片（已上传到 ComfyUI 的文件名，可留空）</summary>
    public string ImagePath3 { get; set; } = string.Empty;

    /// <summary>生成提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>输出图片宽度，默认 1920</summary>
    public int Width { get; set; } = 1920;

    /// <summary>输出图片高度，默认 1080</summary>
    public int Height { get; set; } = 1080;

    /// <summary>是否启用 Lightning LoRA（4步快速生成），默认 true</summary>
    public bool EnableLightningLora { get; set; } = true;

    /// <summary>随机种子（默认 -1 表示随机）</summary>
    public long Seed { get; set; } = -1;
}
