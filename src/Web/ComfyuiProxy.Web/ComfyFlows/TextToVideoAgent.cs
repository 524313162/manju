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
        foreach (var (nodeId, node) in workflow)
        {
            if (node is not JsonObject nodeObj) continue;

            var classType = nodeObj["class_type"]?.GetValue<string>();

            switch (classType)
            {
                case "CLIPTextEncode":
                    if (parameters.TryGetValue("prompt", out var prompt))
                        nodeObj["inputs"]!["text"] = JsonValue.Create(prompt.ToString());
                    break;
            }
        }
    }
}
