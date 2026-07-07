using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public class StableBgmAgent : ComfyUIAgentBase<StableBgmRequestDto, StableBgmResponse>
{
    public StableBgmAgent(ComfyuiProxyService proxyService, ILogger<StableBgmAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "stable-bgm-generate";
    public override string WorkflowFileName => "09.STABLE-BGM.json";

    protected override async Task<string> BuildWorkflowJsonAsync(StableBgmRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var bgmNode = workflow["52:31"]?.AsObject();
        if (bgmNode != null)
        {
            var inputs = bgmNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["value"] = dto.Prompt;
            }
        }

        var durationNode = workflow["52:36"]?.AsObject();
        if (durationNode != null)
        {
            var inputs = durationNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["value"] = dto.Duration ?? 150;
            }
        }

        return workflow.ToJsonString();
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

            var audioArray = nodeOutput["audio"]?.AsArray();
            if (audioArray == null) continue;

            foreach (var audio in audioArray)
            {
                var audioObj = audio?.AsObject();
                if (audioObj == null) continue;
                var filename = audioObj["filename"]?.GetValue<string>();
                var subfolder = audioObj["subfolder"]?.GetValue<string>();
                var type = audioObj["type"]?.GetValue<string>() ?? "output";
                if (!string.IsNullOrEmpty(filename))
                {
                    result.AudioUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}&type={type}");
                }
            }
        }
    }
}
