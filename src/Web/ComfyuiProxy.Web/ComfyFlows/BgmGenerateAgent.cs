using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 09.STABLE-BGM 背景音乐生成 Agent
/// </summary>
public class BgmGenerateAgent : ComfyUIAgentBase
{
    public BgmGenerateAgent(ComfyuiProxyService proxyService, ILogger<BgmGenerateAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "stable-bgm-generate";
    public override string WorkflowFileName => "09.STABLE-BGM-背景音乐.json";

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
                if (widgetsValues == null || widgetsValues.Count == 0) continue;

                switch (type)
                {
                    case "CLIPTextEncode":
                        if (parameters.TryGetValue("prompt", out var prompt))
                            widgetsValues[0] = JsonValue.Create(prompt.ToString());
                        break;

                    case "INT":
                        if (parameters.TryGetValue("duration", out var duration))
                            widgetsValues[0] = JsonValue.Create(Convert.ToInt32(duration));
                        break;
                }
            }
        }

        return ComfyuiProxyService.ConvertUiWorkflowToApiJson(workflow);
    }
}
