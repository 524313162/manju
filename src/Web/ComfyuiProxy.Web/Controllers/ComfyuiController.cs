using ManjuCraft.Domain.Models;
using ComfyuiProxy.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/v1/comfyui")]
public class ComfyuiController : ControllerBase
{
    private readonly ComfyuiProxyService _proxy;
    private readonly TaskManager _taskMgr;

    public ComfyuiController(ComfyuiProxyService proxy, TaskManager taskMgr)
    {
        _proxy = proxy;
        _taskMgr = taskMgr;
    }

    [HttpGet("status")]
    public async Task<IActionResult> CheckStatus()
    {
        var status = await _proxy.CheckComfyuiStatusAsync();
        return Ok(new { success = true, status, data = status });
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var status = await _proxy.CheckComfyuiStatusAsync();
        return Ok(new
        {
            success = true,
            status,
            version = "1.0.0",
            comfyui = status ? "connected" : "disconnected",
            queueLength = _taskMgr.QueueLength
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
                    _taskMgr.Update(taskId, "failed", 0, error: error);
                    return;
                }

                _taskMgr.Update(taskId, "running", 10, node: promptId);

                var outputs = await _proxy.PollAsync(promptId);
                var outputPath = await _proxy.DownloadOutputAsync(outputs);

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
                _taskMgr.Update(taskId, "failed", 0, error: "任务超时");
            }
            catch (Exception ex)
            {
                _taskMgr.Update(taskId, "failed", 0, error: ex.Message);
            }
        });

        return Ok(new
        {
            success = true,
            data = new
            {
                taskId,
                status = "queued",
                progress = 0
            }
        });
    }

    [HttpGet("tasks")]
    public IActionResult GetAllTasks()
    {
        var tasks = _taskMgr.All().OrderByDescending(t => t.CreatedAt).ToList();
        return Ok(new { success = true, data = tasks });
    }

    [HttpGet("tasks/{id}")]
    public IActionResult GetTask(string id)
    {
        var task = _taskMgr.Get(id);
        if (task == null) return NotFound(new { success = false, error = "任务不存在" });

        return Ok(new { success = true, data = task });
    }
}
