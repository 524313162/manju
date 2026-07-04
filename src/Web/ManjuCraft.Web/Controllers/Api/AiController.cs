using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.AI;

namespace ManjuCraft.Web.Controllers.Api;

[Route("api/v1/ai")]
[ApiController]
public class AiController : ControllerBase
{
    private readonly IAiTextService _text;
    private readonly IAiMediaService _media;

    public AiController(IAiTextService text, IAiMediaService media)
    {
        _text = text;
        _media = media;
    }

    [HttpPost("story/generate")]
    public async Task<IActionResult> GenerateStory([FromForm] string title, [FromForm] string prompt, [FromForm] long? projectId = null)
    {
        var data = await _text.GenerateStoryAsync(title, prompt, projectId);
        return Ok(new { success = true, data = data, storyId = projectId });
    }

    [HttpPost("story/rewrite")]
    public async Task<IActionResult> RewriteStory([FromForm] string prompt, [FromForm] string originalStory, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.RewriteStoryAsync(prompt, originalStory, projectId) });

    [HttpPost("assets/extract")]
    public async Task<IActionResult> ExtractAssets([FromForm] string story, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.ExtractAssetsAsync(story, projectId) });

    [HttpPost("character/profile")]
    public async Task<IActionResult> CreateCharacterProfile([FromForm] string description, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.CreateCharacterProfileAsync(description, projectId) });

    [HttpPost("scene/profile")]
    public async Task<IActionResult> CreateSceneProfile([FromForm] string description, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.CreateSceneProfileAsync(description, projectId) });

    [HttpPost("prop/profile")]
    public async Task<IActionResult> CreatePropProfile([FromForm] string description, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.CreatePropProfileAsync(description, projectId) });

    [HttpPost("skill/profile")]
    public async Task<IActionResult> CreateSkillProfile([FromForm] string description, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.CreateSkillProfileAsync(description, projectId) });

    [HttpPost("bgm/generate")]
    public async Task<IActionResult> CreateBgm([FromForm] string description, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.CreateBgmPromptAsync(description, projectId) });

    [HttpPost("video/generate")]
    public async Task<IActionResult> CreateVideo([FromForm] string description, [FromForm] string? referenceImages = null, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _text.CreateVideoPromptAsync(description, referenceImages?.Split(',').ToList(), projectId) });

    [HttpPost("image/txt2img")]
    public async Task<IActionResult> TextToImage([FromForm] string prompt, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _media.TextToImageAsync(prompt, projectId) });

    [HttpPost("image/generate")]
    public async Task<IActionResult> GenerateImage([FromForm] string profile, [FromForm] string assetType, [FromForm] long? projectId = null)
        => Ok(new { success = true, data = await _media.GenerateImageAsync(profile, assetType, projectId) });
}
