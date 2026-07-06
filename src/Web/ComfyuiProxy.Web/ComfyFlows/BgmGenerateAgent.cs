using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 09.STABLE-BGM 背景音乐生成 Agent
/// </summary>
public class BgmGenerateAgent : ComfyUIAgentBase<StableBgmRequestDto>
{
    public BgmGenerateAgent(ComfyuiProxyService proxyService, ILogger<BgmGenerateAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "stable-bgm-generate";
    public override string WorkflowFileName => "09.STABLE-BGM-背景音乐.json";


    protected override async Task<string> BuildWorkflowJsonAsync(StableBgmRequestDto dto)
    {

        return string.Empty;
    }
}
