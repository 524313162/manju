using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/comfyui/ltx")]
public class LtxController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public LtxController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    [HttpPost("text-to-video")]
    public async Task<ActionResult<object>> TextToVideo(
        [FromBody] LtxTextToVideoRequestDto dto)
    {
        var agent = (LtxTextToVideoAgent)_agentFactory.GetAgent("ltx-text-to-video");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "ltx-text-to-video" });
    }

    [HttpPost("image-to-video")]
    public async Task<ActionResult<object>> ImageToVideo(
        [FromBody] LtxImageToVideoRequestDto dto)
    {
        var agent = (LtxImageToVideoAgent)_agentFactory.GetAgent("ltx-image-to-video");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "ltx-image-to-video" });
    }
}
