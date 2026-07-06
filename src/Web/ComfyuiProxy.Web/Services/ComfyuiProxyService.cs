using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ComfyuiProxy.Web.Services;

/// <summary>
/// ComfyUI HTTP 代理核心服务
/// 负责与 ComfyUI 服务器通信：执行工作流、查询状态、获取结果、上传文件
/// </summary>
public class ComfyuiProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ComfyuiProxyService> _logger;

    private const string DefaultComfyuiUrl = "http://localhost:8188";

    public ComfyuiProxyService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ComfyuiProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>获取 ComfyUI 基础 URL</summary>
    public string GetBaseUrl() =>
        _configuration["ComfyUI:Url"] ?? DefaultComfyuiUrl;

    /// <summary>获取工作流文件存放目录</summary>
    public string GetWorkflowsDir()
    {
        var dir = _configuration["ComfyUI:WorkflowsDir"];
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            return dir;

        // 默认路径：相对于项目目录
        var defaultDir = Path.Combine(Directory.GetCurrentDirectory(), "Workflows");
        if (!Directory.Exists(defaultDir))
            Directory.CreateDirectory(defaultDir);
        return defaultDir;
    }

    /// <summary>
    /// 执行工作流：提交 JSON 到 ComfyUI 的 /prompt 接口
    /// </summary>
    /// <param name="workflowJson">完整的工作流 JSON</param>
    /// <returns>prompt_id</returns>
    public async Task<string> ExecuteWorkflowAsync(string workflowJson)
    {
        var baseUrl = GetBaseUrl();
        var client = _httpClientFactory.CreateClient("http");

        var content = new StringContent(workflowJson, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{baseUrl}/prompt", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonObject>(responseBody);
        var promptId = result?["prompt_id"]?.GetValue<string>();

        if (string.IsNullOrEmpty(promptId))
            throw new InvalidOperationException($"ComfyUI 未返回 prompt_id: {responseBody}");

        _logger.LogInformation("工作流已提交，prompt_id: {PromptId}", promptId);
        return promptId;
    }


    /// <summary>
    /// 等待工作流执行完成并获取结果
    /// </summary>
    /// <param name="promptId">提示词 ID</param>
    /// <param name="pollIntervalMs">轮询间隔（毫秒）</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>历史记录 JSON</returns>
    public async Task<JsonObject?> WaitForResultAsync(
        string promptId,
        int pollIntervalMs = 2000,
        int timeoutMs = 300_000,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        var client = _httpClientFactory.CreateClient("http");
        var startTime = DateTime.UtcNow;

        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            await Task.Delay(pollIntervalMs);

            var historyResponse = await client.GetAsync($"{baseUrl}/history/{promptId}");
            if (!historyResponse.IsSuccessStatusCode)
                continue;

            var historyBody = await historyResponse.Content.ReadAsStringAsync();
            var history = JsonSerializer.Deserialize<JsonObject>(historyBody);

            if (history != null && history.ContainsKey(promptId))
            {
                _logger.LogInformation("工作流 {PromptId} 执行完成", promptId);
                return history[promptId]?.AsObject();
            }
        }

        _logger.LogWarning("工作流 {PromptId} 执行超时", promptId);
        return null;
    }





    /// <summary>
    /// 上传文件到 ComfyUI
    /// </summary>
    public async Task<string> UploadFileAsync(string filePath, string? subfolder = null, bool overwrite = false)
    {
        var baseUrl = GetBaseUrl();
        var client = _httpClientFactory.CreateClient("http");

        using var form = new MultipartFormDataContent();
        var fileStream = new StreamContent(File.OpenRead(filePath));
        fileStream.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileStream, "image", Path.GetFileName(filePath));

        if (!string.IsNullOrEmpty(subfolder))
            form.Add(new StringContent(subfolder), "subfolder");

        form.Add(new StringContent(overwrite ? "true" : "false"), "overwrite");

        var response = await client.PostAsync($"{baseUrl}/upload", form);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("文件上传成功: {FilePath}", filePath);

        return responseBody;
    }

    /// <summary>
    /// 加载工作流 JSON 文件（原始 UI 格式）
    /// </summary>
    /// <param name="workflowFileName">工作流文件名，如 "01.ZIMAGE-文生图.json"</param>
    /// <returns>工作流 JSON 对象（UI 格式，包含 nodes 数组）</returns>
    public async Task<JsonObject?> LoadWorkflowAsync(string workflowFileName)
    {
        var workflowsDir = GetWorkflowsDir();
        var filePath = Path.Combine(workflowsDir, workflowFileName);

        if (!File.Exists(filePath))
        {
            _logger.LogError("工作流文件不存在: {FilePath}", filePath);
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<JsonObject>(json);
    }
}

/// <summary>
/// ComfyUI 任务状态枚举
/// </summary>
public enum ComfyuiTaskStatus
{
    NotFound,
    Pending,
    Running,
    Completed
}
