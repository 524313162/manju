using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 08.ACE-MUSIC 音乐生成 Agent
/// </summary>
public class MusicComposeAgent : ComfyUIAgentBase<AceMusicRequestDto, AceMusicResponse>
{
    public MusicComposeAgent(ComfyuiProxyService proxyService, ILogger<MusicComposeAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ace-music-compose";
    public override string WorkflowFileName => "08.ACE-MUSIC-音乐生成.json";


    protected override async Task<string> BuildWorkflowJsonAsync(AceMusicRequestDto dto)
    {

        return string.Empty;
    }

    /// <summary>
    /// 音乐生成解析：从 historyItem 中提取音频 URL
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, AceMusicResponse result)
    {
        // TODO: 根据 ComfyUI history 的实际结构提取音频 URL
    }
}
