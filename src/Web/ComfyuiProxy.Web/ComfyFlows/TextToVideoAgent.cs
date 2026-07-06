using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 03.LTX 文生视频 Agent
/// </summary>
public class TextToVideoAgent : ComfyUIAgentBase
{
    public TextToVideoAgent(ComfyuiProxyService proxyService, ILogger<TextToVideoAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ltx-text-to-video";
    public override string WorkflowFileName => "03.LTX-文生视频.json";

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
                if (nodeObj["type"]?.GetValue<string>() != "CLIPTextEncode") continue;

                var widgetsValues = nodeObj["widgets_values"]?.AsArray();
                if (widgetsValues != null && widgetsValues.Count > 0 && parameters.TryGetValue("prompt", out var prompt))
                    widgetsValues[0] = JsonValue.Create(prompt.ToString());
            }
        }

        return ComfyuiProxyService.ConvertUiWorkflowToApiJson(workflow);
    }
}
