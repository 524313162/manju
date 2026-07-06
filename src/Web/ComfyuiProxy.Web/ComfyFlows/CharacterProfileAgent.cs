using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 02.ZIMAGE 人物档案 Agent
/// 工作流结构：外层节点包含一个子图（subgraph），内部有 3 个 CLIPTextEncode 节点：
///   - Node 200：正向提示词（system_prompt）
///   - Node 201：反向提示词（negative_prompt）
///   - Node 202：角色提示词（character_prompt）
/// </summary>
public class CharacterProfileAgent : ComfyUIAgentBase<ZImageCharacterProfileRequestDto>
{
    public CharacterProfileAgent(ComfyuiProxyService proxyService, ILogger<CharacterProfileAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-character-profile";
    public override string WorkflowFileName => "02.ZIMAGE-人物档案.json";


    /// <summary>
    /// 构建工作流 JSON：加载模板 → 展开子图 → 注入参数 → 转 API 格式
    /// </summary>
    protected override async Task<string> BuildWorkflowJsonAsync(ZImageCharacterProfileRequestDto dto)
    {

        return string.Empty;
    }
}
