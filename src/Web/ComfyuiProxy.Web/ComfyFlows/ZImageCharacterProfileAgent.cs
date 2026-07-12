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

        var systemNode = workflow["57:200"]?.AsObject();
        if (systemNode != null && !string.IsNullOrEmpty(dto.SystemPrompt))
        {
            var inputs = systemNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["text"] = dto.SystemPrompt;
            }
        }

        var characterNode = workflow["57:202"]?.AsObject();
        if (characterNode != null && !string.IsNullOrEmpty(dto.CharacterPrompt))
        {
            var inputs = characterNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["text"] = dto.CharacterPrompt;
            }
        }

        var negativeNode = workflow["57:201"]?.AsObject();
        if (negativeNode != null && !string.IsNullOrEmpty(dto.NegativePrompt))
        {
            var inputs = negativeNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["text"] = dto.NegativePrompt;
            }
        }

        var latentNode = workflow["57:72"]?.AsObject();
        if (latentNode != null && dto.Width > 0 && dto.Height > 0)
        {
            var inputs = latentNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["width"] = dto.Width;
                inputs["height"] = dto.Height;
            }
        }

        var seedNode = workflow["57:74"]?.AsObject();
        if (seedNode != null)
        {
            var inputs = seedNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["seed"] = (int)(DateTime.UtcNow.Ticks % int.MaxValue);
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

            var images = nodeOutput["images"]?.AsArray();
            if (images == null) continue;

            foreach (var img in images)
            {
                var imgObj = img?.AsObject();
                if (imgObj == null) continue;
                var filename = imgObj["filename"]?.GetValue<string>();
                var subfolder = imgObj["subfolder"]?.GetValue<string>();
                var type = imgObj["type"]?.GetValue<string>() ?? "output";
                if (!string.IsNullOrEmpty(filename))
                {
                    result.ImageUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}&type={type}");
                }
            }
        }
    }
}
