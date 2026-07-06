using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 04.LTX 图生视频 Agent
/// </summary>
public class ImageToVideoAgent : ComfyUIAgentBase<LtxImageToVideoRequestDto, LtxVideoResponse>
{
    public ImageToVideoAgent(ComfyuiProxyService proxyService, ILogger<ImageToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-image-to-video";
    public override string WorkflowFileName => "04.LTX-图生视频.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LtxImageToVideoRequestDto dto)
    {

        return string.Empty;
    }

    /// <summary>
    /// 图生视频解析：从 historyItem 中提取视频 URL
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, LtxVideoResponse result)
    {
        // TODO: 根据 ComfyUI history 的实际结构提取视频 URL
    }
}
