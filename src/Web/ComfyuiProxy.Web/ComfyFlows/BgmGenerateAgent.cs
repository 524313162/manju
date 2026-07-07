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
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var apiPrompt = ConvertToApiFormat(workflow!);
        var promptObj = apiPrompt["prompt"]?.AsObject();
        if (promptObj == null)
            throw new InvalidOperationException("API prompt 格式异常: 缺少 prompt 字段");

        var bgmNode = promptObj["52"]?.AsObject();
        if (bgmNode != null)
        {
            var inputs = bgmNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["user_input"] = dto.Prompt;
                inputs["duration"] = dto.Duration ?? 150;
            }
        }

        return apiPrompt.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, StableBgmResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        foreach (var kvp in outputs)
        {
            var nodeOutput = kvp.Value?.AsObject();
            if (nodeOutput == null) continue;

            var className = nodeOutput["class_type"]?.GetValue<string>();
            if (className != "SaveAudioMP3") continue;

            var images = nodeOutput["images"]?.AsArray();
            if (images == null) continue;

            foreach (var audio in images)
            {
                var audioObj = audio?.AsObject();
                if (audioObj == null) continue;
                var filename = audioObj["filename"]?.GetValue<string>();
                var subfolder = audioObj["subfolder"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(filename))
                {
                    result.AudioUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}");
                }
            }
        }
    }
}
