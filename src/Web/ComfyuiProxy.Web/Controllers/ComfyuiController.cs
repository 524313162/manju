using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/comfyui")]
public class ComfyuiController : ControllerBase
{
    private readonly ComfyuiProxyService _proxyService;
    private readonly ComfyUIAgentFactory _agentFactory;

    public ComfyuiController(ComfyuiProxyService proxyService, ComfyUIAgentFactory agentFactory)
    {
        _proxyService = proxyService;
        _agentFactory = agentFactory;
    }

    [HttpGet("result/{promptId}")]
    public async Task<ActionResult<object>> GetResult(
        string promptId,
        [FromQuery] string workflowType)
    {
        var historyItem = await _proxyService.GetHistoryAsync(promptId);
        if (historyItem == null)
            return Ok(new { });

        var agent = _agentFactory.GetAgent(workflowType);
        var parseMethod = agent.GetType().GetMethod("ParseHistory");
        if (parseMethod == null)
            return BadRequest("Agent 不支持 ParseHistory");

        var outputs = parseMethod.Invoke(agent, [historyItem]);
        return Ok(outputs);
    }

    [HttpPost("interrupt")]
    public async Task<ActionResult> Interrupt()
    {
        await _proxyService.InterruptAsync();
        return Ok(new { message = "已中断当前任务" });
    }

    [HttpPost("queue/delete")]
    public async Task<ActionResult> DeleteFromQueue(
        [FromBody] DeleteQueueRequestDto dto)
    {
        await _proxyService.DeleteFromQueueAsync(dto.PromptIds);
        return Ok(new { message = $"已删除 {dto.PromptIds.Count} 个任务" });
    }

    [HttpDelete("history/{promptId}")]
    public async Task<ActionResult> DeleteHistory(string promptId)
    {
        await _proxyService.DeleteHistoryAsync(promptId);
        return Ok(new { message = $"已删除历史记录: {promptId}" });
    }
}
