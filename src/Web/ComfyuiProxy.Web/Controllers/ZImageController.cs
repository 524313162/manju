using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/comfyui/zimage")]
public class ZImageController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public ZImageController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    [HttpPost("text-to-image")]
    public async Task<ActionResult<object>> TextToImage(
        [FromBody] ZImageTextToImageRequestDto dto)
    {
        var agent = (ZImageTextToImageAgent)_agentFactory.GetAgent("zimage-text-to-image");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "zimage-text-to-image" });
    }

    [HttpPost("character-profile")]
    public async Task<ActionResult<object>> CharacterProfile(
        [FromBody] ZImageCharacterProfileRequestDto dto)
    {
        var agent = (ZImageCharacterProfileAgent)_agentFactory.GetAgent("zimage-character-profile");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "zimage-character-profile" });
    }
}
