using ComfyuiProxy.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// ComfyUI 代理控制器，仅处理请求转发和响应返回，不包含任何业务逻辑
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ComfyuiController : ControllerBase
{
    private readonly ComfyuiProxyService _comfyuiProxyService;
    private readonly ILogger<ComfyuiController> _logger;

    public ComfyuiController(
        ComfyuiProxyService comfyuiProxyService,
        ILogger<ComfyuiController> logger)
    {
        _comfyuiProxyService = comfyuiProxyService;
        _logger = logger;
    }

    /// <summary>
    /// 执行 ComfyUI 工作流
    /// </summary>
    /// <param name="promptId">工作流 ID</param>
    /// <param name="workflowJson">工作流 JSON 数据</param>
    /// <returns>ComfyUI 执行结果</returns>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteWorkflow([FromQuery] string promptId, [FromBody] string workflowJson)
    {
        try
        {
            _logger.LogInformation("Executing ComfyUI workflow with prompt ID: {PromptId}", promptId);

            var response = await _comfyuiProxyService.ExecuteWorkflowAsync(promptId, workflowJson);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ComfyUI workflow execution returned non-success status: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ComfyUI workflow: {Message}", ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取工作流执行状态
    /// </summary>
    /// <param name="promptId">工作流 ID</param>
    /// <returns>执行状态信息</returns>
    [HttpGet("status/{promptId}")]
    public async Task<IActionResult> GetWorkflowStatus([FromRoute] string promptId)
    {
        try
        {
            _logger.LogInformation("Getting ComfyUI workflow status for prompt ID: {PromptId}", promptId);

            var response = await _comfyuiProxyService.GetWorkflowStatusAsync(promptId);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ComfyUI workflow status check returned non-success status: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ComfyUI workflow status: {Message}", ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取 ComfyUI 系统信息
    /// </summary>
    /// <returns>系统信息</returns>
    [HttpGet("system-info")]
    public async Task<IActionResult> GetSystemInfo()
    {
        try
        {
            _logger.LogInformation("Getting ComfyUI system info");

            var response = await _comfyuiProxyService.GetSystemInfoAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ComfyUI system info request returned non-success status: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ComfyUI system info: {Message}", ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 上传文件到 ComfyUI
    /// </summary>
    /// <param name="subfolder">子文件夹</param>
    /// <param name="overwrite">是否覆盖</param>
    /// <returns>上传结果</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromQuery] string? subfolder = null, [FromQuery] bool overwrite = false)
    {
        try
        {
            if (Request.Form.Files.Count == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            var file = Request.Form.Files[0];
            var tempFilePath = Path.GetTempFileName();

            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File uploaded to temp location: {FileName}", file.FileName);

                var response = await _comfyuiProxyService.UploadFileAsync(tempFilePath, subfolder, overwrite);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ComfyUI file upload returned non-success status: {StatusCode}", response.StatusCode);
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to ComfyUI: {Message}", ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}