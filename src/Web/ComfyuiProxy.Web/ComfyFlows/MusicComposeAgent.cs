using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 08.ACE-MUSIC 音乐生成 Agent
/// </summary>
public class MusicComposeAgent : ComfyUIAgentBase<AceMusicRequestDto>
{
    public MusicComposeAgent(ComfyuiProxyService proxyService, ILogger<MusicComposeAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ace-music-compose";
    public override string WorkflowFileName => "08.ACE-MUSIC-音乐生成.json";


    protected override async Task<string> BuildWorkflowJsonAsync(AceMusicRequestDto dto)
    {

        return string.Empty;
    }
}
