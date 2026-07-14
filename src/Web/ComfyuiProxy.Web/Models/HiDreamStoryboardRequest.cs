namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 07.HIDREAM分镜 请求
/// </summary>
public class HiDreamStoryboardRequestDto
{
    /// <summary>提示词（必填）</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>参考图片路径（可选）</summary>
    public string? ImagePath { get; set; }

    /// <summary>生成宽度（默认1216，16:9比例）</summary>
    public int Width { get; set; } = 1216;

    /// <summary>生成高度（默认684，16:9比例）</summary>
    public int Height { get; set; } = 684;

    /// <summary>随机种子（默认 -1 表示随机）</summary>
    public long Seed { get; set; } = -1;
}
