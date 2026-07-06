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
        [FromBody] StableBgmRequest request,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("stable-bgm-generate");
        var parameters = new Dictionary<string, object>
        {
            ["prompt"] = request.Prompt
        };

        if (request.Duration.HasValue)
            parameters["duration"] = request.Duration.Value;

        var result = await agent.ExecuteAsync(parameters, cancellationToken);

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
