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

        var ltxNode = workflow["267:266"]?.AsObject();
        if (ltxNode != null)
        {
            var inputs = ltxNode["inputs"]?.AsObject();
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
