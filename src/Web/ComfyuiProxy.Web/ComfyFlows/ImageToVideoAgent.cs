using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 04.LTX 图生视频 Agent
/// </summary>
public class ImageToVideoAgent : ComfyUIAgentBase
{
    public ImageToVideoAgent(ComfyuiProxyService proxyService, ILogger<ImageToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-image-to-video";
    public override string WorkflowFileName => "04.LTX-图生视频.json";

    public override void InjectParameters(JsonObject workflow, Dictionary<string, object> parameters)
    {
        // 不再使用，由 BuildWorkflowJsonAsync 完全接管
    }

    protected override async Task<string> BuildWorkflowJsonAsync(Dictionary<string, object> parameters)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var nodes = workflow["nodes"]?.AsArray();
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                if (node is not JsonObject nodeObj) continue;
                var type = nodeObj["type"]?.GetValue<string>();
                var widgetsValues = nodeObj["widgets_values"]?.AsArray();

                switch (type)
                {
                    case "CLIPTextEncode":
                        if (widgetsValues != null && widgetsValues.Count > 0 && parameters.TryGetValue("prompt", out var prompt))
                            widgetsValues[0] = JsonValue.Create(prompt.ToString());
                        break;

                    case "LoadImage":
                        if (widgetsValues != null && widgetsValues.Count > 0 && parameters.TryGetValue("image_path", out var imagePath))
                            widgetsValues[0] = JsonValue.Create(imagePath.ToString());
                        break;
                }
            }
        }

        return ComfyuiProxyService.ConvertUiWorkflowToApiJson(workflow);
    }
}
