using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

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
    /// 背景英语生成
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("generate")]
    public async Task<ActionResult<object>> Generate(
        [FromBody] StableBgmRequestDto dto)
    {
        var agent = (StableBgmAgent)_agentFactory.GetAgent("stable-bgm-generate");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "stable-bgm-generate" });
    }
}
