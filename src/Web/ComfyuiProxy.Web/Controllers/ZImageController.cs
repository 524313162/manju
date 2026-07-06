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
        [FromBody] ZImageTextToImageRequest request,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("zimage-text-to-image");
        var parameters = new Dictionary<string, object>
        {
            ["prompt"] = request.Prompt
        };

        if (request.Width.HasValue)
            parameters["width"] = request.Width.Value;
        if (request.Height.HasValue)
            parameters["height"] = request.Height.Value;

        var result = await agent.ExecuteAsync(parameters, cancellationToken);

        return Ok(new ZImageTextToImageResponse
        {
            PromptId = result.PromptId,
            ImageUrls = result.ImageUrls,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }

    /// <summary>
    /// 人物档案
    /// POST /api/comfyui/zimage/character-profile
    /// </summary>
    [HttpPost("character-profile")]
    public async Task<ActionResult<CharacterProfileResponse>> CharacterProfile(
        [FromBody] ZImageCharacterProfileRequest request,
        CancellationToken cancellationToken)
    {
        var agent = _agentFactory.GetAgent("zimage-character-profile");
        var parameters = new Dictionary<string, object>
        {
            ["system_prompt"] = request.SystemPrompt,
            ["character_prompt"] = request.CharacterPrompt,
            ["negative_prompt"] = request.NegativePrompt
        };

        var result = await agent.ExecuteAsync(parameters, cancellationToken);

        return Ok(new CharacterProfileResponse
        {
            PromptId = result.PromptId,
            ImageUrls = result.ImageUrls,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Success = result.Success,
            Error = result.Error
        });
    }
}
