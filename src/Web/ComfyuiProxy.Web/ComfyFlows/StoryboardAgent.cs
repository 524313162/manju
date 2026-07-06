using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 07.HIDREAM 分镜 Agent
/// </summary>
public class StoryboardAgent : ComfyUIAgentBase<HiDreamStoryboardRequestDto>
{
    public StoryboardAgent(ComfyuiProxyService proxyService, ILogger<StoryboardAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "hidream-storyboard";
    public override string WorkflowFileName => "07.HIDREAM-分镜.json";


    protected override async Task<string> BuildWorkflowJsonAsync(HiDreamStoryboardRequestDto dto)
    {

        return string.Empty;
    }
}
