using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 20.LLM-QWen 大语言模型 Agent
/// </summary>
public class LlmQwenAgent : ComfyUIAgentBase<LlmQwenRequestDto, LlmQwenResponse>
{
    public LlmQwenAgent(ComfyuiProxyService proxyService, ILogger<LlmQwenAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "llm-qwen-execute";
    public override string WorkflowFileName => "20.LLM-QWen.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LlmQwenRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        // 1. 通用转换：UI 格式 -> API 格式
        var apiPrompt = ConvertToApiFormat(workflow!);

        // 2. 注入动态参数
        var promptObj = apiPrompt["prompt"]?.AsObject();
        if (promptObj == null)
            throw new InvalidOperationException("API prompt 格式异常: 缺少 prompt 字段");

        // 注入到 TextGenerate 节点 (node 7)
        var textGenNode = promptObj["7"]?.AsObject();
        if (textGenNode != null)
        {
            var inputs = textGenNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["prompt"] = dto.Prompt;
                inputs["max_length"] = dto.MaxLength ?? 2048;
            }
        }

        return apiPrompt.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, LlmQwenResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        foreach (var kvp in outputs)
        {
            var nodeOutput = kvp.Value?.AsObject();
            if (nodeOutput == null) continue;

            var className = nodeOutput["class_type"]?.GetValue<string>();
            if (className != "TextGenerate") continue;

            var generatedText = nodeOutput["generated_text"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(generatedText))
            {
                result.Text = generatedText;
                return;
            }
        }
    }
}
