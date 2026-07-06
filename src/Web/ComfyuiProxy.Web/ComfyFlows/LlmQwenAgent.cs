using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 20.LLM-QWen 大语言模型 Agent
/// </summary>
public class LlmQwenAgent : ComfyUIAgentBase<LlmQwenRequestDto>
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

        var nodes = workflow["nodes"]?.AsArray();
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                if (node is not JsonObject nodeObj) continue;
                if (nodeObj["type"]?.GetValue<string>() != "TextGenerate") continue;

                var widgetsValues = nodeObj["widgets_values"]?.AsArray();
                if (widgetsValues == null) continue;

                // widgets_values[0] = system prompt（用户传入的 prompt 替换此位置）
                if (widgetsValues.Count > 0)
                    widgetsValues[0] = JsonValue.Create(dto.Prompt);

                // widgets_values[1] = max_length
                if (dto.MaxLength.HasValue && widgetsValues.Count > 1)
                    widgetsValues[1] = JsonValue.Create(dto.MaxLength.Value);
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// LLM 特殊解析：提取文本输出
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, WorkflowExecutionResult result)
    {
        // 先调用基类解析
        base.ParseOutputs(historyItem, result);

        // LLM 的文本输出可能在 text 字段中
        if (result.TextOutputs.Count == 0)
        {
            var outputs = historyItem["outputs"]?.AsObject();
            if (outputs != null)
            {
                foreach (var (_, nodeOutput) in outputs)
                {
                    // 尝试从各种可能的字段提取文本
                    var textFields = new[] { "text" };
                    foreach (var field in textFields)
                    {
                        if (nodeOutput?[field] != null)
                        {
                            var text = nodeOutput[field]?[0]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(text))
                            {
                                result.TextOutputs.Add(text);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
