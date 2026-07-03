using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.LLM;

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
    public async Task<IActionResult> GenerateStory([FromForm] string title, [FromForm] string prompt)
        => Ok(new { success = true, data = await _text.GenerateStoryAsync(title, prompt) });

    [HttpPost("story/rewrite")]
    public async Task<IActionResult> RewriteStory([FromForm] string prompt, [FromForm] string originalStory)
        => Ok(new { success = true, data = await _text.RewriteStoryAsync(prompt, originalStory) });

    [HttpPost("assets/extract")]
    public async Task<IActionResult> ExtractAssets([FromForm] string story)
        => Ok(new { success = true, data = await _text.ExtractAssetsAsync(story) });

    [HttpPost("character/profile")]
    public async Task<IActionResult> CreateCharacterProfile([FromForm] string description)
        => Ok(new { success = true, data = await _text.CreateCharacterProfileAsync(description) });

    [HttpPost("scene/profile")]
    public async Task<IActionResult> CreateSceneProfile([FromForm] string description)
        => Ok(new { success = true, data = await _text.CreateSceneProfileAsync(description) });

    [HttpPost("prop/profile")]
    public async Task<IActionResult> CreatePropProfile([FromForm] string description)
        => Ok(new { success = true, data = await _text.CreatePropProfileAsync(description) });

    [HttpPost("skill/profile")]
    public async Task<IActionResult> CreateSkillProfile([FromForm] string description)
        => Ok(new { success = true, data = await _text.CreateSkillProfileAsync(description) });

    [HttpPost("bgm/generate")]
    public async Task<IActionResult> CreateBgm([FromForm] string description)
        => Ok(new { success = true, data = await _text.CreateBgmPromptAsync(description) });

    [HttpPost("video/generate")]
    public async Task<IActionResult> CreateVideo([FromForm] string description, [FromForm] string? referenceImages = null)
        => Ok(new { success = true, data = await _text.CreateVideoPromptAsync(description, referenceImages?.Split(',').ToList()) });

    [HttpPost("image/txt2img")]
    public async Task<IActionResult> TextToImage([FromForm] string prompt)
        => Ok(new { success = true, data = await _media.TextToImageAsync(prompt) });

    [HttpPost("image/generate")]
    public async Task<IActionResult> GenerateImage([FromForm] string profile, [FromForm] string assetType)
        => Ok(new { success = true, data = await _media.GenerateImageAsync(profile, assetType) });
}