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
        // TODO 根据 WorkflowFileName 具体的内容进行读取,正对当前这个工作流的进行参数适配
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        return string.Empty;
    }

    /// <summary>
    /// LLM 特殊解析：从 historyItem 中提取文本输出
    /// </summary>
    protected override void ParseOutputs(JsonObject historyItem, LlmQwenResponse result)
    {
        // TODO: 根据 ComfyUI history 的实际结构解析文本输出
        // 示例：从指定节点的 outputs.text 中提取
        // var outputs = historyItem["outputs"]?.AsObject();
        // result.Text = outputs?["node_id"]?["text"]?.GetValue<string>() ?? string.Empty;
    }
}
