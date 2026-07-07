using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 04.LTX 图生视频 Agent
/// </summary>
public class ImageToVideoAgent : ComfyUIAgentBase<LtxImageToVideoRequestDto, LtxVideoResponse>
{
    public ImageToVideoAgent(ComfyuiProxyService proxyService, ILogger<ImageToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-image-to-video";
    public override string WorkflowFileName => "04.LTX-图生视频.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LtxImageToVideoRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var apiPrompt = ConvertToApiFormat(workflow!);
        var promptObj = apiPrompt["prompt"]?.AsObject();
        if (promptObj == null)
            throw new InvalidOperationException("API prompt 格式异常: 缺少 prompt 字段");

        // 注入 text 参数到 LTX 模型节点 (node 269)
        var ltxNode = promptObj["269"]?.AsObject();
        if (ltxNode != null)
        {
            var inputs = ltxNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["value"] = dto.ImagePath;
                inputs["value_1"] = dto.Prompt;
            }
        }

        return apiPrompt.ToJsonString();
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
