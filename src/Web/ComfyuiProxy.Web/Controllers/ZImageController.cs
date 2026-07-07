using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// ZIMAGE 工作流控制器（文生图 + 人物档案）
/// </summary>
[ApiController]
[Route("api/comfyui/zimage")]
public class ZImageController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public ZImageController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// 文生图
    /// POST /api/comfyui/zimage/text-to-image
    /// </summary>
    [HttpPost("text-to-image")]
    public async Task<ActionResult<ZImageTextToImageResponse>> TextToImage(
        [FromBody] ZImageTextToImageRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = (ZImageTextToImageAgent)_agentFactory.GetAgent("zimage-text-to-image");
        var result = await agent.ExecuteAsync(dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 人物档案
    /// POST /api/comfyui/zimage/character-profile
    /// </summary>
    [HttpPost("character-profile")]
    public async Task<ActionResult<CharacterProfileResponse>> CharacterProfile(
        [FromBody] ZImageCharacterProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var agent = (ZImageCharacterProfileAgent)_agentFactory.GetAgent("zimage-character-profile");
        var result = await agent.ExecuteAsync(dto, cancellationToken);
        return Ok(result);
    }
}
