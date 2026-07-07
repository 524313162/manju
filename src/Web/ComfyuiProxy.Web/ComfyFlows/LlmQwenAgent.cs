using ComfyuiProxy.Web.Services;
using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

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

        var apiPrompt = ConvertToApiFormat(workflow!);

        var promptObj = apiPrompt["prompt"]?.AsObject();
        if (promptObj == null)
            throw new InvalidOperationException("API prompt 格式异常: 缺少 prompt 字段");

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

    protected override JsonObject ConvertToApiFormat(JsonObject uiWorkflow)
    {
        var uiOnlyTypes = new HashSet<string> { "MarkdownNote", "Note", "PrimitiveNode" };

        var apiPrompt = new JsonObject();
        var apiNodes = new JsonObject();
        var nodesArray = uiWorkflow["nodes"]?.AsArray();
        if (nodesArray == null)
            throw new InvalidOperationException("工作流文件中未找到 nodes 数组");

        var linkIndex = new Dictionary<int, JsonArray>();
        var linksArray = uiWorkflow["links"]?.AsArray();
        if (linksArray != null)
        {
            foreach (var linkEntry in linksArray)
            {
                if (linkEntry == null) continue;
                var linkArr = linkEntry.AsArray();
                var linkId = linkArr[0]?.GetValue<int>();
                if (linkId.HasValue)
                {
                    linkIndex[linkId.Value] = linkArr;
                }
            }
        }

        foreach (var node in nodesArray)
        {
            if (node == null) continue;
            var nodeObj = node.AsObject();
            var nodeId = nodeObj["id"]?.GetValue<int>();
            if (nodeId == null) continue;

            var classType = nodeObj["type"]?.GetValue<string>() ?? string.Empty;

            if (uiOnlyTypes.Contains(classType))
                continue;

            var apiNode = new JsonObject();
            var inputs = new JsonObject();

            apiNode["class_type"] = classType;

            if (nodeObj["_meta"] != null)
            {
                apiNode["_meta"] = nodeObj["_meta"]!;
            }

            var widgetIndex = 0;
            var inputsArray = nodeObj["inputs"]?.AsArray();

            if (inputsArray != null)
            {
                foreach (var input in inputsArray)
                {
                    if (input == null) continue;
                    var inputObj = input.AsObject();
                    var inputName = inputObj["name"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(inputName)) continue;

                    var link = inputObj["link"];
                    if (link != null)
                    {
                        var linkId = link.GetValue<int>();
                        if (linkIndex.TryGetValue(linkId, out var linkArr))
                        {
                            var fromNodeId = linkArr[1]!.ToString()!;
                            var fromSlot = linkArr[2]!;

                            var linkInfoArr = new JsonArray { fromNodeId };
                            if (fromSlot.GetValueKind() == System.Text.Json.JsonValueKind.Number)
                                linkInfoArr.Add(fromSlot.GetValue<int>());
                            else
                                linkInfoArr.Add(fromSlot.GetValue<string>());

                            inputs[inputName] = linkInfoArr;
                        }
                    }
                    else
                    {
                        var widget = inputObj["widget"];
                        if (widget != null)
                        {
                            var wv = nodeObj["widgets_values"]?.AsArray();
                            if (wv != null && widgetIndex < wv.Count)
                            {
                                inputs[inputName] = wv[widgetIndex]?.DeepClone();
                            }
                            widgetIndex++;
                        }
                    }
                }
            }

            apiNode["inputs"] = inputs;
            apiNodes[$"{nodeId}"] = apiNode;
        }

        apiPrompt["prompt"] = apiNodes;
        return apiPrompt;
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
