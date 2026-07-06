using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 03.LTX 文生视频 Agent
/// </summary>
public class TextToVideoAgent : ComfyUIAgentBase<LtxTextToVideoRequestDto>
{
    public TextToVideoAgent(ComfyuiProxyService proxyService, ILogger<TextToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-text-to-video";
    public override string WorkflowFileName => "03.LTX-文生视频.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LtxTextToVideoRequestDto dto)
    {

        return string.Empty;
    }
}
