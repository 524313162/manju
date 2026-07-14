using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/comfyui/llm-qwen")]
public class LlmQwenController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public LlmQwenController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// 文本LLM处理大模型，推理
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("execute")]
    public async Task<ActionResult<object>> Execute(
        [FromBody] LlmQwenRequestDto dto)
    {
        var agent = (LlmQwenAgent)_agentFactory.GetAgent("llm-qwen-execute");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "llm-qwen-execute" });
    }
}
