using System.Net.Http;
using System.Text;
using System.Text.Json;

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
}