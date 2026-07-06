using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// HIDREAM 分镜控制器
/// </summary>
[ApiController]
[Route("api/comfyui/hidream")]
public class HiDreamController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public HiDreamController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// 分镜生成
    /// POST /api/comfyui/hidream/storyboard
    /// </summary>
    [HttpPost("storyboard")]
    public async Task<ActionResult<StoryboardResponse>> Storyboard(
        [FromBody] HiDreamStoryboardRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("hidream-storyboard");
        var result = await agent.ExecuteAsync(dto, cancellationToken);

        return Ok(new StoryboardResponse
        {
            PromptId = result.PromptId,
            ImageUrls = result.ImageUrls,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }
}
