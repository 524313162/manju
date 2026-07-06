using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// LTX 工作流控制器（文生视频 + 图生视频）
/// </summary>
[ApiController]
[Route("api/comfyui/ltx")]
public class LtxController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public LtxController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// 文生视频
    /// POST /api/comfyui/ltx/text-to-video
    /// </summary>
    [HttpPost("text-to-video")]
    public async Task<ActionResult<LtxVideoResponse>> TextToVideo(
        [FromBody] LtxTextToVideoRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("ltx-text-to-video");
        var result = await agent.ExecuteAsync(dto, cancellationToken);

        return Ok(new LtxVideoResponse
        {
            PromptId = result.PromptId,
            VideoUrls = result.VideoUrls,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }

    /// <summary>
    /// 图生视频
    /// POST /api/comfyui/ltx/image-to-video
    /// </summary>
    [HttpPost("image-to-video")]
    public async Task<ActionResult<LtxVideoResponse>> ImageToVideo(
        [FromBody] LtxImageToVideoRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("ltx-image-to-video");
        var result = await agent.ExecuteAsync(dto, cancellationToken);

        return Ok(new LtxVideoResponse
        {
            PromptId = result.PromptId,
            VideoUrls = result.VideoUrls,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }
}
