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
        // 不再使用，由 BuildWorkflowJsonAsync 完全接管
    }

    /// <summary>
    /// 构建工作流 JSON：加载模板 → 展开子图 → 注入参数 → 转 API 格式
    /// </summary>
    protected override async Task<string> BuildWorkflowJsonAsync(Dictionary<string, object> parameters)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        // 工作流是 ComfyUI 子图格式，将子图内部的 nodes 和 links 提升到顶层
        var subgraph = workflow["definitions"]?["subgraphs"]?.AsArray()?[0]?.AsObject();
        if (subgraph != null)
        {
            if (subgraph["nodes"] is JsonArray subgraphNodes)
                workflow["nodes"] = subgraphNodes.DeepClone().AsArray();
            if (subgraph["links"] is JsonArray subgraphLinks)
                workflow["links"] = subgraphLinks.DeepClone().AsArray();
        }

        // 注入参数到 CLIPTextEncode 节点
        var nodes = workflow["nodes"]?.AsArray();
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                if (node is not JsonObject nodeObj) continue;
                if (nodeObj["type"]?.GetValue<string>() != "CLIPTextEncode") continue;

                var widgetsValues = nodeObj["widgets_values"]?.AsArray();
                if (widgetsValues == null || widgetsValues.Count == 0) continue;

                var title = nodeObj["title"]?.GetValue<string>() ?? "";

                switch (title)
                {
                    case "正向提示词":
                        if (parameters.TryGetValue("system_prompt", out var sp))
                            widgetsValues[0] = JsonValue.Create(sp.ToString());
                        break;
                    case "反向提示词":
                        if (parameters.TryGetValue("negative_prompt", out var np))
                            widgetsValues[0] = JsonValue.Create(np.ToString());
                        break;
                    case "角色提示词":
                        if (parameters.TryGetValue("character_prompt", out var cp))
                            widgetsValues[0] = JsonValue.Create(cp.ToString());
                        break;
                }
            }
        }

        return ComfyuiProxyService.ConvertUiWorkflowToApiJson(workflow);
    }
}
