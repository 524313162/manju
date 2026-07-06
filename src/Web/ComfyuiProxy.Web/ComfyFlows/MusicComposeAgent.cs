using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 08.ACE-MUSIC 音乐生成 Agent
/// </summary>
public class MusicComposeAgent : ComfyUIAgentBase
{
    public MusicComposeAgent(ComfyuiProxyService proxyService, ILogger<MusicComposeAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ace-music-compose";
    public override string WorkflowFileName => "08.ACE-MUSIC-音乐生成.json";

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

                case "StringLiteral":
                    if (parameters.TryGetValue("lyrics", out var lyrics))
                        nodeObj["inputs"]!["string"] = JsonValue.Create(lyrics.ToString());
                    break;
            }
        }
    }
}
