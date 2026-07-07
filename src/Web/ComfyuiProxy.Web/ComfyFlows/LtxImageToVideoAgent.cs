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

        var ltxNode = workflow["269"]?.AsObject();
        if (ltxNode != null)
        {
            var inputs = ltxNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["image"] = dto.ImagePath;
            }
        }

        var promptNode = workflow["320:319"]?.AsObject();
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

    protected override void ParseOutputs(JsonObject historyItem, LtxVideoResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        foreach (var kvp in outputs)
        {
            var nodeOutput = kvp.Value?.AsObject();
            if (nodeOutput == null) continue;

            var className = nodeOutput["class_type"]?.GetValue<string>();
            if (className != "SaveVideo") continue;

            var images = nodeOutput["images"]?.AsArray();
            if (images == null) continue;

            foreach (var video in images)
            {
                var videoObj = video?.AsObject();
                if (videoObj == null) continue;
                var filename = videoObj["filename"]?.GetValue<string>();
                var subfolder = videoObj["subfolder"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(filename))
                {
                    result.VideoUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}");
                }
            }
        }
    }
}
