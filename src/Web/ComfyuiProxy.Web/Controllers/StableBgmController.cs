using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// STABLE-BGM 背景音乐生成控制器
/// </summary>
[ApiController]
[Route("api/comfyui/stable-bgm")]
public class StableBgmController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public StableBgmController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// 背景音乐生成
    /// POST /api/comfyui/stable-bgm/generate
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<StableBgmResponse>> Generate(
        [FromBody] StableBgmRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("stable-bgm-generate");
        var result = await agent.ExecuteAsync(dto, cancellationToken);

        return Ok(new StableBgmResponse
        {
            PromptId = result.PromptId,
            AudioUrls = result.AudioUrls,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }
}
