using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 20.LLM-QWen 大语言模型 Agent
/// </summary>
public class LlmQwenAgent : ComfyUIAgentBase
{
    public LlmQwenAgent(ComfyuiProxyService proxyService, ILogger<LlmQwenAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "llm-qwen-execute";
    public override string WorkflowFileName => "20.LLM-QWen.json";

    public override void InjectParameters(JsonObject workflow, Dictionary<string, object> parameters)
    {
        // UI 格式：遍历 nodes 数组，按 type 匹配节点
        var nodes = workflow["nodes"]?.AsArray();
        if (nodes == null) return;

        foreach (var node in nodes)
        {
            if (node is not JsonObject nodeObj) continue;

            var type = nodeObj["type"]?.GetValue<string>();

            if (type == "TextGenerate")
            {
                var widgetsValues = nodeObj["widgets_values"]?.AsArray();
                if (widgetsValues == null) continue;

                // widgets_values[0] = system prompt（用户传入的 prompt 替换此位置）
                if (parameters.TryGetValue("prompt", out var prompt) && widgetsValues.Count > 0)
                {
                    widgetsValues[0] = JsonValue.Create(prompt.ToString());
                }

                // widgets_values[1] = max_length
                if (parameters.TryGetValue("max_length", out var maxLength) && widgetsValues.Count > 1)
                {
                    widgetsValues[1] = JsonValue.Create(Convert.ToInt32(maxLength));
                }
            }
        }
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
                    var textFields = new[] { "text", "response", "output", "result", "string" };
                    foreach (var field in textFields)
                    {
                        if (nodeOutput?[field] != null)
                        {
                            var text = nodeOutput[field]?.GetValue<string>();
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
