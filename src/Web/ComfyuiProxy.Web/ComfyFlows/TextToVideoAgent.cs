using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 03.LTX 文生视频 Agent
/// </summary>
public class TextToVideoAgent : ComfyUIAgentBase<LtxTextToVideoRequestDto, LtxVideoResponse>
{
    public TextToVideoAgent(ComfyuiProxyService proxyService, ILogger<TextToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-text-to-video";
    public override string WorkflowFileName => "03.LTX-文生视频.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LtxTextToVideoRequestDto dto)
    {
        // TODO 根据 WorkflowFileName 具体的内容进行读取,正对当前这个工作流的进行参数适配

        return string.Empty;
    }

    /// <summary>
    /// 文生视频解析：从 historyItem 中提取视频 URL
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, LtxVideoResponse result)
    {
        // TODO: 根据 ComfyUI history 的实际结构提取视频 URL
    }
}
