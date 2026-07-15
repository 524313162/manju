using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public class LtxImageToVideoAgent : ComfyUIAgentBase<LtxImageToVideoRequestDto, LtxVideoResponse>
{
    public LtxImageToVideoAgent(ComfyuiProxyService proxyService, ILogger<LtxImageToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-image-to-video";
    public override string WorkflowFileName => "04.LTX图生视频.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LtxImageToVideoRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var imageNode = workflow["269"]?.AsObject();
        if (imageNode != null)
        {
            var inputs = imageNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["image"] = dto.ImagePath;
        }

        var promptNode = workflow["320:319"]?.AsObject();
        if (promptNode != null)
        {
            var inputs = promptNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Prompt;
        }

        var widthNode = workflow["320:312"]?.AsObject();
        if (widthNode != null)
        {
            var inputs = widthNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Width;
        }

        var heightNode = workflow["320:299"]?.AsObject();
        if (heightNode != null)
        {
            var inputs = heightNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Height;
        }

        var fpsNode = workflow["320:300"]?.AsObject();
        if (fpsNode != null)
        {
            var inputs = fpsNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Fps;
        }

        var durationNode = workflow["320:301"]?.AsObject();
        if (durationNode != null)
        {
            var inputs = durationNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Duration;
        }

        var seed = dto.Seed >= 0 ? dto.Seed : (int)(DateTime.UtcNow.Ticks % int.MaxValue);      

        var seedNode = workflow["320:277"]?.AsObject();
        if (seedNode != null)
        {
            var inputs = seedNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["noise_seed"] = seed;
        }

        return workflow.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, LtxVideoResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        var videoNode = outputs["75"]?.AsObject();
        if (videoNode == null)
            return;

        var images = videoNode["images"]?.AsArray();
        if (images == null)
            return;

        foreach (var video in images)
        {
            var videoObj = video?.AsObject();
            if (videoObj == null) continue;
            var filename = videoObj["filename"]?.GetValue<string>();
            var subfolder = videoObj["subfolder"]?.GetValue<string>();
            var type = videoObj["type"]?.GetValue<string>() ?? "output";
            if (!string.IsNullOrEmpty(filename))
            {
                result.VideoUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}&type={type}");
            }
        }
    }
}
