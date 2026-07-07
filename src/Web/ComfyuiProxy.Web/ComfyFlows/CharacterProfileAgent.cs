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
public class CharacterProfileAgent : ComfyUIAgentBase<ZImageCharacterProfileRequestDto, CharacterProfileResponse>
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
        // TODO 根据 WorkflowFileName 具体的内容进行读取,正对当前这个工作流的进行参数适配

        return string.Empty;
    }

    /// <summary>
    /// 人物档案解析：从 historyItem 中提取图片 URL
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, CharacterProfileResponse result)
    {
        // TODO: 根据 ComfyUI history 的实际结构提取图片 URL
        // var outputs = historyItem["outputs"]?.AsObject();
        // var images = outputs?["node_id"]?["images"]?.AsArray();
        // if (images != null)
        // {
        //     foreach (var img in images)
        //     {
        //         var filename = img?["filename"]?.GetValue<string>();
        //         var subfolder = img?["subfolder"]?.GetValue<string>();
        //         result.ImageUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}");
        //     }
        // }
    }
}
