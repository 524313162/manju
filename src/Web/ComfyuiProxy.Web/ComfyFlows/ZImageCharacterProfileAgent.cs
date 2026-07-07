using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public class ZImageCharacterProfileAgent : ComfyUIAgentBase<ZImageCharacterProfileRequestDto, CharacterProfileResponse>
{
    public ZImageCharacterProfileAgent(ComfyuiProxyService proxyService, ILogger<ZImageCharacterProfileAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-character-profile";
    public override string WorkflowFileName => "02.ZIMAGE-人物档案.json";

    protected override async Task<string> BuildWorkflowJsonAsync(ZImageCharacterProfileRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var generateNode = workflow["57:200"]?.AsObject();
        if (generateNode != null)
        {
            var inputs = generateNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["clip_name"] = "qwen_3_4b.safetensors";
                inputs["unet_name"] = "z_image_turbo_bf16.safetensors";
                inputs["vae_name"] = "ae.safetensors";
                inputs["width"] = 576;
                inputs["height"] = 1024;
                inputs["seed"] = 0;
                inputs["steps"] = 4;
            }
        }

        return workflow.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, CharacterProfileResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        foreach (var kvp in outputs)
        {
            var nodeOutput = kvp.Value?.AsObject();
            if (nodeOutput == null) continue;

            var className = nodeOutput["class_type"]?.GetValue<string>();
            if (className != "SaveImage") continue;

            var images = nodeOutput["images"]?.AsArray();
            if (images == null) continue;

            foreach (var img in images)
            {
                var imgObj = img?.AsObject();
                if (imgObj == null) continue;
                var filename = imgObj["filename"]?.GetValue<string>();
                var subfolder = imgObj["subfolder"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(filename))
                {
                    result.ImageUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}");
                }
            }
        }
    }
}
