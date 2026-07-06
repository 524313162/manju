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
        // 遍历所有节点，找到需要注入参数的节点
        foreach (var (nodeId, node) in workflow)
        {
            if (node is not JsonObject nodeObj) continue;

            var classType = nodeObj["class_type"]?.GetValue<string>();

            // 根据 class_type 注入参数到对应节点
            switch (classType)
            {
                case "CLIPTextEncode" when parameters.ContainsKey("prompt"):
                    nodeObj["inputs"]!["text"] = JsonValue.Create(parameters["prompt"].ToString());
                    break;

                case "EmptyLatentImage":
                    if (parameters.TryGetValue("width", out var width))
                        nodeObj["inputs"]!["width"] = JsonValue.Create(Convert.ToInt32(width));
                    if (parameters.TryGetValue("height", out var height))
                        nodeObj["inputs"]!["height"] = JsonValue.Create(Convert.ToInt32(height));
                    break;
            }
        }
    }
}
