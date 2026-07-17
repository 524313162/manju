using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// QWen 图生图 (Image Edit 2509) 工作流 Agent
/// 对应工作流文件：07.QWen-图生图2.json
/// 使用 Qwen Image Edit 模型进行图片编辑生成
/// </summary>
public class QwenImageEditAgent : ComfyUIAgentBase<QwenImageEditRequestDto, QwenImageEditResponse>
{
    public QwenImageEditAgent(ComfyuiProxyService proxyService, ILogger<QwenImageEditAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "qwen-image-edit";
    public override string WorkflowFileName => "07.QWen-图生图2.json";

    /// <summary>
    /// 构建工作流 JSON
    /// </summary>
    protected override async Task<string> BuildWorkflowJsonAsync(QwenImageEditRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        // 节点 78：LoadImage — 替换主输入图片（图1）
        var imageNode1 = workflow["78"]?.AsObject();
        if (imageNode1 != null)
        {
            var inputs = imageNode1["inputs"]?.AsObject();
            if (inputs != null)
                inputs["image"] = dto.ImagePath1;
        }

        // 节点 79：LoadImage — 替换第二张图片（图2）
        var imageNode2 = workflow["79"]?.AsObject();
        if (imageNode2 != null)
        {
            var inputs = imageNode2["inputs"]?.AsObject();
            if (inputs != null)
                inputs["image"] = dto.ImagePath2;
        }

        // 节点 80：LoadImage — 替换第三张图片（图3）
        var imageNode3 = workflow["80"]?.AsObject();
        if (imageNode3 != null)
        {
            var inputs = imageNode3["inputs"]?.AsObject();
            if (inputs != null)
                inputs["image"] = dto.ImagePath3;
        }

        // 节点 435：PrimitiveStringMultiline — 替换提示词
        var promptNode = workflow["435"]?.AsObject();
        if (promptNode != null)
        {
            var inputs = promptNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.Prompt;
        }

        // 节点 433:3：KSampler — 设置种子
        var samplerNode = workflow["433:3"]?.AsObject();
        if (samplerNode != null)
        {
            var inputs = samplerNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["seed"] = dto.Seed >= 0 ? dto.Seed : (int)(DateTime.UtcNow.Ticks % int.MaxValue);
        }

        // 节点 433:445：ImageScale — 设置输出尺寸
        var sizeNode = workflow["433:445"]?.AsObject();
        if (sizeNode != null)
        {
            var inputs = sizeNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["width"] = dto.Width;
                inputs["height"] = dto.Height;
            }
        }

        // 节点 433:443：PrimitiveBoolean — 设置 Lightning LoRA 开关
        var lightningNode = workflow["433:443"]?.AsObject();
        if (lightningNode != null)
        {
            var inputs = lightningNode["inputs"]?.AsObject();
            if (inputs != null)
                inputs["value"] = dto.EnableLightningLora;
        }

        return workflow.ToJsonString();
    }

    /// <summary>
    /// 从 ComfyUI 历史记录中解析输出图片
    /// 输出节点 60：SaveImage
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, QwenImageEditResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        var imageNode = outputs["60"]?.AsObject();
        if (imageNode == null)
            return;

        var images = imageNode["images"]?.AsArray();
        if (images == null)
            return;

        foreach (var img in images)
        {
            var imgObj = img?.AsObject();
            if (imgObj == null) continue;
            var filename = imgObj["filename"]?.GetValue<string>();
            var subfolder = imgObj["subfolder"]?.GetValue<string>();
            var type = imgObj["type"]?.GetValue<string>() ?? "output";
            if (!string.IsNullOrEmpty(filename))
            {
                result.ImageUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}&type={type}");
            }
        }
    }
}
