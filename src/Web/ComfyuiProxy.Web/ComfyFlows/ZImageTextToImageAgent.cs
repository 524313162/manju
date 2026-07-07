using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public class ZImageTextToImageAgent : ComfyUIAgentBase<ZImageTextToImageRequestDto, ZImageTextToImageResponse>
{
    public ZImageTextToImageAgent(ComfyuiProxyService proxyService, ILogger<ZImageTextToImageAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-text-to-image";
    public override string WorkflowFileName => "01.ZIMAGE文生图.json";

    protected override async Task<string> BuildWorkflowJsonAsync(ZImageTextToImageRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var clipTextNode = workflow["57:27"]?.AsObject();
        if (clipTextNode != null)
        {
            var inputs = clipTextNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["text"] = dto.Prompt;
            }
        }

        var emptyLatentNode = workflow["57:13"]?.AsObject();
        if (emptyLatentNode != null)
        {
            var inputs = emptyLatentNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["width"] = dto.Width;
                inputs["height"] = dto.Height;
            }
        }

        return workflow.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, ZImageTextToImageResponse result)
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
