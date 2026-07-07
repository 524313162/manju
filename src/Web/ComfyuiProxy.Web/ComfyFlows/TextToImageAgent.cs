using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 01.ZIMAGE 文生图 Agent
/// </summary>
public class TextToImageAgent : ComfyUIAgentBase<ZImageTextToImageRequestDto, ZImageTextToImageResponse>
{
    public TextToImageAgent(ComfyuiProxyService proxyService, ILogger<TextToImageAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "zimage-text-to-image";
    public override string WorkflowFileName => "01.ZIMAGE-文生图.json";

    protected override async Task<string> BuildWorkflowJsonAsync(ZImageTextToImageRequestDto dto)
    {
        // TODO 根据 WorkflowFileName 具体的内容进行读取,正对当前这个工作流的进行参数适配

        return string.Empty;
    }

    /// <summary>
    /// 文生图解析：从 historyItem 中提取图片 URL
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, ZImageTextToImageResponse result)
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
