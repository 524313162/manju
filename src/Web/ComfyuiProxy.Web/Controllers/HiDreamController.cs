using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

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
    /// 分镜生成请求，图片为多张参考图合并在一张上用来生成分镜。
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("storyboard")]
    public async Task<ActionResult<object>> Storyboard(
        [FromBody] HiDreamStoryboardRequestDto dto)
    {
        var agent = (HiDreamStoryboardAgent)_agentFactory.GetAgent("hidream-storyboard");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "hidream-storyboard" });
    }
}
