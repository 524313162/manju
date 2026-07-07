using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 09.STABLE-BGM 背景音乐生成 Agent
/// </summary>
public class BgmGenerateAgent : ComfyUIAgentBase<StableBgmRequestDto, StableBgmResponse>
{
    public BgmGenerateAgent(ComfyuiProxyService proxyService, ILogger<BgmGenerateAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "stable-bgm-generate";
    public override string WorkflowFileName => "09.STABLE-BGM-背景音乐.json";


    protected override async Task<string> BuildWorkflowJsonAsync(StableBgmRequestDto dto)
    {
        // TODO 根据 WorkflowFileName 具体的内容进行读取,正对当前这个工作流的进行参数适配
        return string.Empty;
    }

    /// <summary>
    /// 背景音乐解析：从 historyItem 中提取音频 URL
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, StableBgmResponse result)
    {
        // TODO: 根据 ComfyUI history 的实际结构提取音频 URL
    }
}
