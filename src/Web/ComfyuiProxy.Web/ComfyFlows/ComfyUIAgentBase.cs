using System.Text.Json;
using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// ComfyUI Agent 基类
/// 提供公共的工作流执行逻辑：加载模板、注入参数、提交执行、等待结果、解析输出
/// </summary>
public abstract class ComfyUIAgentBase<TParams, TResult> : IComfyUIAgent<TParams, TResult>
    where TParams : class
    where TResult : ComfyUIResponseBase, new()
{
    protected readonly ComfyuiProxyService _proxyService;
    protected readonly ILogger _logger;

    protected ComfyUIAgentBase(ComfyuiProxyService proxyService, ILogger logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    /// <summary>工作流类型标识</summary>
    public abstract string WorkflowType { get; }

    /// <summary>工作流文件名</summary>
    public abstract string WorkflowFileName { get; }

    /// <summary>
    /// 构建工作流 JSON（加载模板 + 注入参数）
    /// 子类必须实现此方法，直接返回 API 格式的 JSON 字符串
    /// </summary>
    protected abstract Task<string> BuildWorkflowJsonAsync(TParams dto);

    /// <summary>
    /// 将 ComfyUI UI 格式的工作流 JSON 转换为 API 格式（/prompt 接口所需格式）
    /// UI 格式: nodes[].inputs[].link=id -> API 格式: inputs[name]=[from_node_id, output_slot]
    /// UI 格式: nodes[].inputs[].widget -> API 格式: inputs[fieldName]=widgets_values[index]
    /// </summary>
    protected virtual JsonObject ConvertToApiFormat(JsonObject uiWorkflow)
    {
        var apiPrompt = new JsonObject();
        var apiNodes = new JsonObject();
        var nodesArray = uiWorkflow["nodes"]?.AsArray();
        if (nodesArray == null)
            throw new InvalidOperationException("工作流文件中未找到 nodes 数组");

        foreach (var node in nodesArray)
        {
            if (node == null) continue;
            var nodeObj = node.AsObject();
            var nodeId = nodeObj["id"]?.GetValue<int>();
            if (nodeId == null) continue;

            var apiNode = new JsonObject();
            var inputs = new JsonObject();

            // class_type 在 API 格式中是必选字段
            var classType = nodeObj["type"]?.GetValue<string>() ?? string.Empty;
            apiNode["class_type"] = classType;

            // 复制 _meta 字段（如果存在）
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
                        // 有连接的输入: link 是 link id，需要根据 links 数组反查 [from_node_id, from_output]
                        var linkId = link.GetValue<int>();
                        var linksArray = uiWorkflow["links"]?.AsArray();

                        if (linksArray != null)
                        {
                            foreach (var linkEntry in linksArray)
                            {
                                if (linkEntry == null) continue;
                                var linkArr = linkEntry.AsArray();
                                // link entry 格式: [link_id, from_node_id, from_output, to_node_id, to_input_slot, type]
                                if (linkArr[0]?.GetValue<int>() == linkId)
                                {
                                    var linkInfoArr = new JsonArray
                                    {
                                        linkArr[1]!.ToString()!,
                                        linkArr[2]!.ToString()!
                                    };
                                    inputs[inputName] = linkInfoArr;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // widget 输入：从 widgets_values 中按顺序取值
                        var widget = inputObj["widget"];
                        if (widget != null)
                        {
                            var wv = nodeObj["widgets_values"]?.AsArray();
                            if (wv != null && widgetIndex < wv.Count)
                            {
                                inputs[inputName] = wv[widgetIndex];
                            }
                            widgetIndex++;
                        }
                    }
                }
            }

            apiNode["inputs"] = inputs;

            // 添加 widgets_values 到 API 格式中
            var widgetsValues = nodeObj["widgets_values"]?.AsArray();
            if (widgetsValues != null && widgetsValues.Count > 0)
            {
                apiNode["widgets_values"] = widgetsValues;
            }

            apiNodes[$"{nodeId}"] = apiNode;
        }

        apiPrompt["prompt"] = apiNodes;
        return apiPrompt;
    }

    /// <summary>
    /// 执行工作流的完整流程：构建 workflowJson → 提交执行 → 等待结果 → 解析输出
    /// </summary>
    /// <param name="parameters">请求参数（各 Agent 对应的 Request DTO）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>强类型执行结果（TResult）</returns>
    public async Task<TResult> ExecuteAsync(
        TParams parameters,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new TResult();

        try
        {
            // 1. 构建工作流 JSON（加载模板 + 注入参数 + 转 API 格式）
            var workflowJson = await BuildWorkflowJsonAsync(parameters);

            // 2. 提交到 ComfyUI 执行
            _logger.LogInformation("[{WorkflowType}] 提交工作流到 ComfyUI", WorkflowType);
            var promptId = await _proxyService.ExecuteWorkflowAsync(workflowJson);

            // 3. 等待执行完成
            _logger.LogInformation("[{WorkflowType}] 等待执行完成, prompt_id: {PromptId}", WorkflowType, promptId);
            var historyItem = await _proxyService.WaitForResultAsync(promptId, cancellationToken: cancellationToken);
            if (historyItem == null)
            {
                result.Success = false;
                result.Error = "工作流执行超时";
                result.ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                return result;
            }

            // 4. 解析输出 — 子类将填充 TResult 的具体字段
            ParseOutputs(historyItem, result);

            result.PromptId = promptId;
            result.Success = true;
            result.ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("[{WorkflowType}] 工作流执行成功, 耗时: {Time:F0}ms",
                WorkflowType, (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "请求被取消";
            result.ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{WorkflowType}] 工作流执行异常: {Message}", WorkflowType, ex.Message);
            result.Success = false;
            result.Error = ex.Message;
            result.ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 解析历史记录中的输出到结果对象
    /// 子类必须实现此方法，从 historyItem 中提取输出并填充到 result 中
    /// </summary>
    protected abstract void ParseOutputs(JsonObject historyItem, TResult result);
}

/// <summary>
/// ComfyUI 响应结果基类
/// 所有 Agent 的 Response DTO 必须继承此类，包含公共字段
/// </summary>
public abstract class ComfyUIResponseBase
{
    /// <summary>ComfyUI 提示词 ID</summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>错误信息（失败时）</summary>
    public string? Error { get; set; }

    /// <summary>执行耗时（毫秒）</summary>
    public double ExecutionTimeMs { get; set; }
}
