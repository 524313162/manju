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

        var samplerNode = workflow["108"]?.AsObject();
        if (samplerNode != null)
        {
            var inputs = samplerNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["noise_seed"] = dto.Seed >= 0 ? dto.Seed : (int)(DateTime.UtcNow.Ticks % int.MaxValue);
            }
        }

        var latentNode = workflow["156"]?.AsObject();
        if (latentNode != null)
        {
            var inputs = latentNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["width"] = dto.Width;
                inputs["height"] = dto.Height;
            }
        }

        if (!string.IsNullOrEmpty(dto.ImagePath))
        {
            var imageNode = workflow["213"]?.AsObject();
            if (imageNode != null)
            {
                var inputs = imageNode["inputs"]?.AsObject();
                if (inputs != null)
                {
                    inputs["image"] = dto.ImagePath;
                }
            }
        }

        return workflow.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, StoryboardResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        var imageNode = outputs["227"]?.AsObject();
        if (imageNode == null)
            return;

        var images = imageNode["images"]?.AsArray();
        if (images == null)
            return;

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
