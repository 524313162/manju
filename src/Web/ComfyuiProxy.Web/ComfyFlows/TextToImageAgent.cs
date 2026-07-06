using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 01.ZIMAGE 文生图 Agent
/// </summary>
public class TextToImageAgent : ComfyUIAgentBase<ZImageTextToImageRequestDto>
{
    public TextToImageAgent(ComfyuiProxyService proxyService, ILogger<TextToImageAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-text-to-image";
    public override string WorkflowFileName => "01.ZIMAGE-文生图.json";

    protected override async Task<string> BuildWorkflowJsonAsync(ZImageTextToImageRequestDto dto)
    {

        return string.Empty;
    }
}
