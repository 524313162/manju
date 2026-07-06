using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 02.ZIMAGE 人物档案 Agent
/// </summary>
public class CharacterProfileAgent : ComfyUIAgentBase
{
    public CharacterProfileAgent(ComfyuiProxyService proxyService, ILogger<CharacterProfileAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-character-profile";
    public override string WorkflowFileName => "02.ZIMAGE-人物档案.json";

    public override void InjectParameters(JsonObject workflow, Dictionary<string, object> parameters)
    {
        foreach (var (nodeId, node) in workflow)
        {
            if (node is not JsonObject nodeObj) continue;

            var classType = nodeObj["class_type"]?.GetValue<string>();

            switch (classType)
            {
                case "CLIPTextEncode":
                    var text = nodeObj["inputs"]?["text"]?.GetValue<string>() ?? "";
                    if (text.Contains("{system_prompt}") && parameters.TryGetValue("system_prompt", out var sp))
                        nodeObj["inputs"]!["text"] = JsonValue.Create(text.Replace("{system_prompt}", sp.ToString()));
                    else if (text.Contains("{character_prompt}") && parameters.TryGetValue("character_prompt", out var cp))
                        nodeObj["inputs"]!["text"] = JsonValue.Create(text.Replace("{character_prompt}", cp.ToString()));
                    break;

                case "CLIPTextEncode (negative)":
                    if (parameters.TryGetValue("negative_prompt", out var np))
                        nodeObj["inputs"]!["text"] = JsonValue.Create(np.ToString());
                    break;
            }
        }
    }
}
