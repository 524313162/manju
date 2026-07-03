using ComfyuiProxy.Web.Services;
using ManjuCraft.Domain.Models;
using ManjuCraft.Domain.Models.ComfyUI;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/v1/comfyui")]
[Produces("application/json")]
public class ComfyuiController : ControllerBase
{
    private readonly ComfyuiProxyService _proxy;
    private readonly TaskManager _taskMgr;
    private readonly Serilog.ILogger _logger;

    public ComfyuiController(
        ComfyuiProxyService proxy,
        TaskManager taskMgr,
        Serilog.ILogger logger)
    {
        _proxy = proxy;
        _taskMgr = taskMgr;
        _logger = logger;
    }

    /// <summary>
    /// 检查ComfyUI服务状态
    /// </summary>
    /// <returns>服务状态信息</returns>
    [HttpGet("status")]
    public async Task<IActionResult> CheckStatus()
    {
        var status = await _proxy.CheckComfyuiStatusAsync();
        return Ok(new { success = true, status, message = status ? "ComfyUI服务正常运行" : "ComfyUI服务连接失败" });
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var comfyuiOk = await _proxy.CheckComfyuiStatusAsync();
        return Ok(new
        {
            success = true,
            status = comfyuiOk ? "ok" : "degraded",
            version = "1.0.0",
            comfyui = comfyuiOk ? "connected" : "disconnected",
            queueLength = _taskMgr.QueueLength,
            message = comfyuiOk ? "系统运行正常" : "系统运行异常"
        });
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        var taskId = _taskMgr.Enqueue(request.WorkflowType, request.Prompt);

        _ = Task.Run(async () =>
        {
            try
            {
                var (promptId, error) = await _proxy.SubmitAsync(request);
                if (error != null)
                {
                    _logger.Error("提交任务失败: {Error}", error);
                    _taskMgr.Update(taskId, "failed", 0, error: error);
                    return;
                }

                _taskMgr.Update(taskId, "running", 10, node: promptId);

                var outputs = await _proxy.PollAsync(promptId);
                var outputPath = await _proxy.DownloadOutputAsync(outputs ?? new Dictionary<string, ComfyuiHistoryNodeOutputs>());

                if (!string.IsNullOrEmpty(outputPath))
                {
                    _taskMgr.Update(taskId, "completed", 100, outputPath: outputPath);
                }
                else
                {
                    _taskMgr.Update(taskId, "failed", 0, error: "未找到输出文件");
                }
            }
            catch (TimeoutException)
            {
                _logger.Error("任务 {TaskId} 超时", taskId);
                _taskMgr.Update(taskId, "failed", 0, error: "任务超时");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "任务 {TaskId} 失败", taskId);
                _taskMgr.Update(taskId, "failed", 0, error: ex.Message);
            }
        });

        return Ok(new { success = true, data = new { taskId, status = "queued", progress = 0 } });
    }

    [HttpGet("tasks")]
    public IActionResult GetAllTasks()
    {
        var tasks = _taskMgr.All().OrderByDescending(t => t.CreatedAt).ToList();
        return Ok(new { success = true, data = tasks, message = "任务列表获取成功" });
    }

    [HttpGet("tasks/{id}")]
    public IActionResult GetTask(string id)
    {
        var task = _taskMgr.Get(id);
        if (task == null)
        {
            return NotFound(new { success = false, error = "任务不存在" });
        }

        return Ok(new { success = true, data = task, message = "任务详情获取成功" });
    }
}
