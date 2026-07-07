using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 07.HIDREAM 分镜 Agent
/// </summary>
public class StoryboardAgent : ComfyUIAgentBase<HiDreamStoryboardRequestDto, StoryboardResponse>
{
    public StoryboardAgent(ComfyuiProxyService proxyService, ILogger<StoryboardAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "hidream-storyboard";
    public override string WorkflowFileName => "07.HIDREAM-分镜.json";

    protected override async Task<string> BuildWorkflowJsonAsync(HiDreamStoryboardRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var apiPrompt = ConvertToApiFormat(workflow!);
        var promptObj = apiPrompt["prompt"]?.AsObject();
        if (promptObj == null)
            throw new InvalidOperationException("API prompt 格式异常: 缺少 prompt 字段");

        // 注入 CLIPTextEncode (node 110) 的 text 参数
        var clipTextNode = promptObj["110"]?.AsObject();
        if (clipTextNode != null)
        {
            var inputs = clipTextNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["text"] = dto.Prompt;
            }
        }

        return apiPrompt.ToJsonString();
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
