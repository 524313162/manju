using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public class LtxTextToVideoAgent : ComfyUIAgentBase<LtxTextToVideoRequestDto, LtxVideoResponse>
{
    public LtxTextToVideoAgent(ComfyuiProxyService proxyService, ILogger<LtxTextToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-text-to-video";
    public override string WorkflowFileName => "03.LTX文生视频.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LtxTextToVideoRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var promptNode = workflow["267:266"]?.AsObject();
        if (promptNode != null)
        {
            var inputs = promptNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["value"] = dto.Prompt;
            }
        }

        var widthNode = workflow["267:257"]?.AsObject();
        if (widthNode != null)
        {
            var inputs = widthNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Width;
        }

        var heightNode = workflow["267:258"]?.AsObject();
        if (heightNode != null)
        {
            var inputs = heightNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Height;
        }

        var fpsNode = workflow["267:260"]?.AsObject();
        if (fpsNode != null)
        {
            var inputs = fpsNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Fps;
        }

        var durationNode = workflow["267:225"]?.AsObject();
        if (durationNode != null)
        {
            var inputs = durationNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Duration;
        }

        return workflow.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, LtxVideoResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        foreach (var kvp in outputs)
        {
            var nodeOutput = kvp.Value?.AsObject();
            if (nodeOutput == null) continue;

            var videoArray = nodeOutput["images"]?.AsArray();
            if (videoArray == null) continue;

            foreach (var video in videoArray)
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
}
