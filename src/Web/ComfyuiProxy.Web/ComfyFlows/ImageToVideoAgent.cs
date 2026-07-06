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

                case "LoadImage":
                    if (parameters.TryGetValue("image_path", out var imagePath))
                        nodeObj["inputs"]!["image"] = JsonValue.Create(imagePath.ToString());
                    break;
            }
        }
    }
}
