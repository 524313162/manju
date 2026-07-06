using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// LLM-QWen 大语言模型控制器
/// </summary>
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
    /// 执行 LLM 推理
    /// POST /api/comfyui/llm-qwen/execute
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<LlmQwenResponse>> Execute(
        [FromBody] LlmQwenRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("llm-qwen-execute");
        var result = await agent.ExecuteAsync(dto, cancellationToken);

        return Ok(new LlmQwenResponse
        {
            PromptId = result.PromptId,
            Text = result.TextOutputs.Count > 0 ? result.TextOutputs[0] : string.Empty,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }
}
