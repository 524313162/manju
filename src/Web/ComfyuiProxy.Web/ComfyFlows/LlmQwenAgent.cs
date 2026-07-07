using ComfyuiProxy.Web.Services;
using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

public class LlmQwenAgent : ComfyUIAgentBase<LlmQwenRequestDto, LlmQwenResponse>
{
    public LlmQwenAgent(ComfyuiProxyService proxyService, ILogger<LlmQwenAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "llm-qwen-execute";
    public override string WorkflowFileName => "20.LLM-QWen-文本.json";

    protected override async Task<string> BuildWorkflowJsonAsync(LlmQwenRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var textGenNode = workflow["7"]?.AsObject();
        if (textGenNode != null)
        {
            var inputs = textGenNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["prompt"] = dto.Prompt;
                inputs["max_length"] = dto.MaxLength ?? 2048;
            }
        }

        return workflow.ToJsonString();
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

            var generatedText = nodeOutput["text"]?[0]?.GetValue<string>();
            if (!string.IsNullOrEmpty(generatedText))
            {
                result.Text = generatedText;
                return;
            }
        }
    }
}
