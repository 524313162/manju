using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 01.ZIMAGE 文生图 Agent
/// </summary>
public class TextToImageAgent : ComfyUIAgentBase<ZImageTextToImageRequestDto, ZImageTextToImageResponse>
{
    public TextToImageAgent(ComfyuiProxyService proxyService, ILogger<TextToImageAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-text-to-image";
    public override string WorkflowFileName => "01.ZIMAGE-文生图.json";

    protected override async Task<string> BuildWorkflowJsonAsync(ZImageTextToImageRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var apiPrompt = ConvertToApiFormat(workflow!);
        var promptObj = apiPrompt["prompt"]?.AsObject();
        if (promptObj == null)
            throw new InvalidOperationException("API prompt 格式异常: 缺少 prompt 字段");

        var textToImageNode = promptObj["57"]?.AsObject();
        if (textToImageNode != null)
        {
            var inputs = textToImageNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["text"] = dto.Prompt;
                inputs["width"] = dto.Width ?? 1024;
                inputs["height"] = dto.Height ?? 576;
            }
        }

        return apiPrompt.ToJsonString();
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
