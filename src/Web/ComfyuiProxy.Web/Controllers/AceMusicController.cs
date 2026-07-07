using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/comfyui/ace-music")]
public class AceMusicController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public AceMusicController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    [HttpPost("compose")]
    public async Task<ActionResult<object>> Compose(
        [FromBody] AceMusicRequestDto dto)
    {
        var agent = (AceMusicAgent)_agentFactory.GetAgent("ace-music-compose");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "ace-music-compose" });
    }
}
