using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public class HiDreamStoryboardAgent : ComfyUIAgentBase<HiDreamStoryboardRequestDto, StoryboardResponse>
{
    public HiDreamStoryboardAgent(ComfyuiProxyService proxyService, ILogger<HiDreamStoryboardAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "hidream-storyboard";
    public override string WorkflowFileName => "07.HIDREAM-分镜.json";

    protected override async Task<string> BuildWorkflowJsonAsync(HiDreamStoryboardRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var promptNode = workflow["171"]?.AsObject();
        if (promptNode != null)
        {
            var inputs = promptNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["value"] = dto.Prompt;
            }
        }

        return workflow.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, StoryboardResponse result)
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
