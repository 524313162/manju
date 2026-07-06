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

                case "INT":
                    if (parameters.TryGetValue("duration", out var duration))
                        nodeObj["inputs"]!["value"] = JsonValue.Create(Convert.ToInt32(duration));
                    break;
            }
        }
    }
}
