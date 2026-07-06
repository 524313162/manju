using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 01.ZIMAGE 文生图 Agent
/// </summary>
public class TextToImageAgent : ComfyUIAgentBase
{
    public TextToImageAgent(ComfyuiProxyService proxyService, ILogger<TextToImageAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-text-to-image";
    public override string WorkflowFileName => "01.ZIMAGE-文生图.json";

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

                switch (type)
                {
                    case "CLIPTextEncode" when parameters.ContainsKey("prompt"):
                        var widgetsValues = nodeObj["widgets_values"]?.AsArray();
                        if (widgetsValues != null && widgetsValues.Count > 0)
                            widgetsValues[0] = JsonValue.Create(parameters["prompt"].ToString());
                        break;

                    case "EmptyLatentImage":
                        var vals = nodeObj["widgets_values"]?.AsArray();
                        if (vals == null) break;
                        if (parameters.TryGetValue("width", out var width) && vals.Count > 0)
                            vals[0] = JsonValue.Create(Convert.ToInt32(width));
                        if (parameters.TryGetValue("height", out var height) && vals.Count > 1)
                            vals[1] = JsonValue.Create(Convert.ToInt32(height));
                        break;
                }
            }
        }

        return ComfyuiProxyService.ConvertUiWorkflowToApiJson(workflow);
    }
}
