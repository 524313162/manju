using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// ACE-MUSIC 音乐生成控制器
/// </summary>
[ApiController]
[Route("api/comfyui/ace-music")]
public class AceMusicController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public AceMusicController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// 音乐生成
    /// POST /api/comfyui/ace-music/compose
    /// </summary>
    [HttpPost("compose")]
    public async Task<ActionResult<AceMusicResponse>> Compose(
        [FromBody] AceMusicRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("ace-music-compose");
        var result = await agent.ExecuteAsync(dto, cancellationToken);

        return Ok(new AceMusicResponse
        {
            PromptId = result.PromptId,
            AudioUrls = result.AudioUrls,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }
}
