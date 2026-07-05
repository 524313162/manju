using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.Services;

/// <summary>
/// ComfyUI 代理服务，仅处理请求转发和响应返回，不包含任何业务逻辑
/// </summary>
public class ComfyuiProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ComfyuiProxyService> _logger;
    private readonly IConfiguration _configuration;

    public ComfyuiProxyService(
        HttpClient httpClient,
        ILogger<ComfyuiProxyService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// 代理执行 ComfyUI 工作流
    /// </summary>
    /// <param name="promptId">工作流 ID</param>
    /// <param name="workflowJson">工作流 JSON 数据</param>
    /// <returns>ComfyUI 执行结果</returns>
    public async Task<HttpResponseMessage> ExecuteWorkflowAsync(string promptId, string workflowJson)
    {
        try
        {
            var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
            var endpoint = $"{comfyuiUrl.TrimEnd('/')}/prompt";

            var requestBody = new
            {
                prompt = workflowJson,
                client_id = promptId
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Executing ComfyUI workflow with prompt ID: {PromptId}", promptId);

            var response = await _httpClient.PostAsync(endpoint, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ComfyUI workflow execution failed: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ComfyUI workflow: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 获取工作流执行状态
    /// </summary>
    /// <param name="promptId">工作流 ID</param>
    /// <returns>执行状态信息</returns>
    public async Task<HttpResponseMessage> GetWorkflowStatusAsync(string promptId)
    {
        try
        {
            var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
            var endpoint = $"{comfyuiUrl.TrimEnd('/')}/history/{promptId}";

            _logger.LogInformation("Getting ComfyUI workflow status for prompt ID: {PromptId}", promptId);

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get ComfyUI workflow status: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ComfyUI workflow status: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 获取 ComfyUI 系统信息
    /// </summary>
    /// <returns>系统信息</returns>
    public async Task<HttpResponseMessage> GetSystemInfoAsync()
    {
        try
        {
            var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
            var endpoint = $"{comfyuiUrl.TrimEnd('/')}/system_info";

            _logger.LogInformation("Getting ComfyUI system info");

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get ComfyUI system info: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ComfyUI system info: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 上传文件到 ComfyUI
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="subfolder">子文件夹</param>
    /// <param name="overwrite">是否覆盖</param>
    /// <returns>上传结果</returns>
    public async Task<HttpResponseMessage> UploadFileAsync(string filePath, string? subfolder = null, bool overwrite = false)
    {
        try
        {
            var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
            var endpoint = $"{comfyuiUrl.TrimEnd('/')}/upload";

            using var formData = new MultipartFormDataContent();
            var fileStream = File.OpenRead(filePath);
            var streamContent = new StreamContent(fileStream);
            var fileContent = new ByteArrayContent(await streamContent.ReadAsByteArrayAsync());
            fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/octet-stream");

            formData.Add(fileContent, "file", Path.GetFileName(filePath));

            if (!string.IsNullOrEmpty(subfolder))
            {
                formData.Add(new StringContent(subfolder), "subfolder");
            }

            formData.Add(new StringContent(overwrite.ToString().ToLower()), "overwrite");

            _logger.LogInformation("Uploading file to ComfyUI: {FilePath}", filePath);

            var response = await _httpClient.PostAsync(endpoint, formData);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("File upload failed: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to ComfyUI: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 执行工作流（加载JSON、注入参数、调用ComfyUI、轮询等待结果）
    /// </summary>
    public async Task<WorkflowExecuteResponse> ExecuteWorkflowWithParamsAsync(WorkflowExecuteRequest request)
    {
        // 1. 加载工作流 JSON 文件
        var workflowsDir = _configuration["ComfyUI:WorkflowsDir"] ?? @"D:\Program Files\ComfyUI-Installs\ComfyUI\ComfyUI\user\default\workflows";
        var workflowPath = Path.Combine(workflowsDir, request.WorkflowFileName);
        if (!File.Exists(workflowPath))
            throw new FileNotFoundException($"工作流文件不存在: {request.WorkflowFileName}");

        var workflowJson = await File.ReadAllTextAsync(workflowPath);

        // 2. 注入参数到工作流 JSON
        if (request.Parameters.Count > 0)
        {
            workflowJson = InjectParametersByMapping(workflowJson, request.WorkflowFileName, request.Parameters);
        }

        // 3. 调用 ComfyUI /prompt 提交任务
        var clientId = request.ClientId ?? Guid.NewGuid().ToString("N")[..16];
        var promptId = await SubmitPromptAsync(workflowJson, clientId);

        // 4. 轮询等待结果（超时 5 分钟）
        var result = await PollForResultAsync(promptId, timeoutSeconds: 300);
        result.PromptId = promptId;
        return result;
    }

/// <summary>
    /// 参数注入映射：工作流文件名 → 参数定义列表
    /// 每个参数定义：(节点ID, widgets_values索引, 参数名, 值类型)
    /// </summary>
    private record ParamMapping(int NodeId, int WidgetIndex, string ParamKey, string ValueType);

    private static readonly Dictionary<string, List<ParamMapping>> WorkflowParamMappings = new()
    {
        // 01.ZIMAGE文生图
        //   节点27(CLIPTextEncode): widgets_values[0]=prompt文本
        //   节点13(EmptySD3LatentImage): widgets_values[0]=width, [1]=height
        //   节点3(KSampler): widgets_values[0]=seed, [1]=control_after_generate, [2]=steps
        ["01.ZIMAGE文生图.json"] = new()
        {
            new(27, 0, "text",   "STRING"),
            new(13, 0, "width",  "INT"),
            new(13, 1, "height", "INT"),
            new(3,  0, "seed",   "INT"),
            new(3,  2, "steps",  "INT"),
        },

        // 02.ZIMAGE人物档案
        //   节点200/201/202(CLIPTextEncode×3): widgets_values[0]=正向/角色/反向提示词
        //   节点72(EmptySD3LatentImage): widgets_values[0]=width, [1]=height
        //   节点74(KSampler): widgets_values[0]=seed, [2]=steps
        ["02.ZIMAGE人物档案.json"] = new()
        {
            new(200, 0, "positive_prompt",   "STRING"),
            new(201, 0, "character_prompt",  "STRING"),
            new(202, 0, "negative_prompt",   "STRING"),
            new(72,  0, "width",  "INT"),
            new(72,  1, "height", "INT"),
            new(74,  0, "seed",   "INT"),
            new(74,  2, "steps",  "INT"),
        },

        // 03.LTX文生视频
        //   节点266(PrimitiveStringMultiline): widgets_values[0]=prompt
        //   节点257/258(Primitive): width/height
        //   节点225(Primitive): duration
        //   节点260(Primitive): fps
        //   节点237(Primitive): noise_seed
        //   节点330(Primitive): prompt_enhance (BOOLEAN)
        ["03.LTX文生视频.json"] = new()
        {
            new(266, 0, "prompt",         "STRING"),
            new(257, 0, "width",          "INT"),
            new(258, 0, "height",         "INT"),
            new(225, 0, "duration",       "INT"),
            new(260, 0, "fps",            "INT"),
            new(237, 0, "seed",           "INT"),
            new(330, 0, "prompt_enhance", "BOOLEAN"),
        },

        // 20.LLM-QWen
        //   节点7(TextGenerate): widgets_values[0]=prompt, [1]=max_length, [3]=temperature
        ["20.LLM-QWen.json"] = new()
        {
            new(7, 0, "prompt",     "STRING"),
            new(7, 1, "max_length", "INT"),
            new(7, 3, "temperature","FLOAT"),
        },
    };

    /// <summary>
    /// 根据工作流文件名匹配参数映射表，直接修改节点 widgets_values
    /// 未匹配到映射的工作流保持原始 JSON 不变
    /// </summary>
    private static string InjectParametersByMapping(string workflowJson, string workflowFileName, Dictionary<string, object> parameters)
    {
        if (!WorkflowParamMappings.TryGetValue(workflowFileName, out var mappings))
        {
            // 未配置的工作流，不注入参数，原样返回
            return workflowJson;
        }

        var root = JsonNode.Parse(workflowJson);
        if (root == null) return workflowJson;

        var nodes = root["nodes"]?.AsArray();
        if (nodes == null) return workflowJson;

        foreach (var mapping in mappings)
        {
            if (!parameters.TryGetValue(mapping.ParamKey, out var value))
                continue;

            // 找到目标节点
            foreach (var node in nodes)
            {
                if (node?["id"]?.GetValue<int>() != mapping.NodeId) continue;

                var widgetsValues = node["widgets_values"]?.AsArray();
                if (widgetsValues == null || mapping.WidgetIndex >= widgetsValues.Count) break;

                widgetsValues[mapping.WidgetIndex] = mapping.ValueType switch
                {
                    "INT"     => JsonValue.Create(Convert.ToInt32(value)),
                    "FLOAT"   => JsonValue.Create(Convert.ToDouble(value)),
                    "BOOLEAN" => JsonValue.Create(Convert.ToBoolean(value)),
                    _         => JsonValue.Create(value.ToString()),
                };
                break;
            }
        }

        return root.ToJsonString();
    }

    private async Task<string> SubmitPromptAsync(string workflowJson, string clientId)
    {
        var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
        var endpoint = $"{comfyuiUrl.TrimEnd('/')}/prompt";

        var requestBody = new { prompt = JsonNode.Parse(workflowJson), client_id = clientId };
        var content = new StringContent(
            JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonNode.Parse(responseJson);
        var promptId = result?["prompt_id"]?.GetValue<string>();

        if (string.IsNullOrEmpty(promptId))
            throw new InvalidOperationException("ComfyUI 未返回 prompt_id");

        return promptId;
    }

    private async Task<WorkflowExecuteResponse> PollForResultAsync(string promptId, int timeoutSeconds)
    {
        var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
        var endpoint = $"{comfyuiUrl.TrimEnd('/')}/history/{promptId}";
        var proxyBaseUrl = _configuration["ComfyUI:ProxyBaseUrl"] ?? "";

        var startTime = DateTime.UtcNow;

        while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
        {
            await Task.Delay(2000);

            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) continue;

            var historyJson = await response.Content.ReadAsStringAsync();
            var history = JsonNode.Parse(historyJson);
            var promptData = history?[promptId];
            if (promptData == null) continue;

            // 检查状态
            var status = promptData["status"]?.AsObject();
            if (status != null)
            {
                var completed = status["completed"]?.GetValue<bool>() ?? false;
                var statusStr = status["status_str"]?.GetValue<string>() ?? "";

                if (completed && statusStr == "error")
                {
                    return new WorkflowExecuteResponse
                    {
                        Status = "error",
                        ErrorMessage = "ComfyUI 工作流执行失败"
                    };
                }
            }

            // 检查是否有输出
            var outputs = promptData["outputs"]?.AsObject();
            if (outputs != null && outputs.Count > 0)
            {
                var result = new WorkflowExecuteResponse { Status = "completed" };

                foreach (var (nodeId, nodeOutput) in outputs)
                {
                    var outputObj = nodeOutput?.AsObject();
                    if (outputObj == null) continue;

                    // 处理 images
                    var images = outputObj["images"]?.AsArray();
                    if (images != null)
                    {
                        foreach (var img in images)
                        {
                            var filename = img?["filename"]?.GetValue<string>();
                            var subfolder = img?["subfolder"]?.GetValue<string>() ?? "";
                            var type = img?["type"]?.GetValue<string>() ?? "output";
                            if (filename != null)
                            {
                                result.Outputs.Add(new WorkflowOutputFile
                                {
                                    FileName = filename,
                                    Subfolder = subfolder,
                                    Type = type,
                                    Url = $"{proxyBaseUrl}/api/comfyui/view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={Uri.EscapeDataString(type)}"
                                });
                            }
                        }
                    }

                    // 处理 gifs
                    var gifs = outputObj["gifs"]?.AsArray();
                    if (gifs != null)
                    {
                        foreach (var gif in gifs)
                        {
                            var filename = gif?["filename"]?.GetValue<string>();
                            var subfolder = gif?["subfolder"]?.GetValue<string>() ?? "";
                            var type = gif?["type"]?.GetValue<string>() ?? "output";
                            if (filename != null)
                            {
                                result.Outputs.Add(new WorkflowOutputFile
                                {
                                    FileName = filename,
                                    Subfolder = subfolder,
                                    Type = type,
                                    Url = $"{proxyBaseUrl}/api/comfyui/view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={Uri.EscapeDataString(type)}"
                                });
                            }
                        }
                    }
                }

                if (result.Outputs.Count > 0)
                    return result;
            }
        }

        return new WorkflowExecuteResponse
        {
            Status = "timeout",
            ErrorMessage = $"工作流执行超时（{timeoutSeconds}秒）"
        };
    }

    /// <summary>
    /// 获取工作流执行结果（按 promptId 查询）
    /// </summary>
    public async Task<WorkflowExecuteResponse> GetWorkflowResultAsync(string promptId)
    {
        var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
        var endpoint = $"{comfyuiUrl.TrimEnd('/')}/history/{promptId}";
        var proxyBaseUrl = _configuration["ComfyUI:ProxyBaseUrl"] ?? "";

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var historyJson = await response.Content.ReadAsStringAsync();
        var history = JsonNode.Parse(historyJson);
        var promptData = history?[promptId];

        if (promptData == null)
            return new WorkflowExecuteResponse { Status = "not_found", ErrorMessage = $"未找到 promptId: {promptId}" };

        var result = new WorkflowExecuteResponse { PromptId = promptId };

        // 检查状态
        var status = promptData["status"]?.AsObject();
        if (status != null)
        {
            var completed = status["completed"]?.GetValue<bool>() ?? false;
            var statusStr = status["status_str"]?.GetValue<string>() ?? "";
            result.Status = completed ? (statusStr == "error" ? "error" : "completed") : statusStr;
        }

        // 提取输出
        var outputs = promptData["outputs"]?.AsObject();
        if (outputs != null)
        {
            foreach (var (nodeId, nodeOutput) in outputs)
            {
                var outputObj = nodeOutput?.AsObject();
                if (outputObj == null) continue;

                var images = outputObj["images"]?.AsArray();
                if (images != null)
                {
                    foreach (var img in images)
                    {
                        var filename = img?["filename"]?.GetValue<string>();
                        var subfolder = img?["subfolder"]?.GetValue<string>() ?? "";
                        var type = img?["type"]?.GetValue<string>() ?? "output";
                        if (filename != null)
                            result.Outputs.Add(new WorkflowOutputFile
                            {
                                FileName = filename, Subfolder = subfolder, Type = type,
                                Url = $"{proxyBaseUrl}/api/comfyui/view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={Uri.EscapeDataString(type)}"
                            });
                    }
                }

                var gifs = outputObj["gifs"]?.AsArray();
                if (gifs != null)
                {
                    foreach (var gif in gifs)
                    {
                        var filename = gif?["filename"]?.GetValue<string>();
                        var subfolder = gif?["subfolder"]?.GetValue<string>() ?? "";
                        var type = gif?["type"]?.GetValue<string>() ?? "output";
                        if (filename != null)
                            result.Outputs.Add(new WorkflowOutputFile
                            {
                                FileName = filename, Subfolder = subfolder, Type = type,
                                Url = $"{proxyBaseUrl}/api/comfyui/view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={Uri.EscapeDataString(type)}"
                            });
                    }
                }
            }
        }

        return result;
    }
}