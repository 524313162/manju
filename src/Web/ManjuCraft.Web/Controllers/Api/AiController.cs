using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.Service.ComfyuiProxy;

namespace ManjuCraft.Web.Controllers.Api;

[ApiController]
[Route("api/v1/ai")]
public class AiController : ControllerBase
{
    private readonly IComfyuiProxyService _proxy;

    public AiController(IComfyuiProxyService proxy)
    {
        _proxy = proxy;
    }

    private IActionResult Success(object data) => Ok(new { success = true, data });
    private IActionResult Fail(string message) => Ok(new { success = false, message });

    #region Image Generation

    [HttpPost("image/generate")]
    public async Task<IActionResult> GenerateImage([FromBody] AiImageRequest req)
    {
        if (string.IsNullOrEmpty(req.Prompt))
            return Fail("prompt 不能为空");

        var result = await _proxy.SubmitAndPollAsync<ComfyuiImageListOutput>(
            "api/comfyui/zimage/text-to-image",
            new { prompt = req.Prompt, width = req.Width ?? 1024, height = req.Height ?? 768 },
            "zimage-text-to-image");

        if (string.IsNullOrEmpty(result.promptId) && result.result?.Urls?.Count == 0)
            return Fail("生成失败");

        if (string.IsNullOrEmpty(result.promptId))
            return Success(new { message = "生成中...", promptId = result.promptId });

        if (string.IsNullOrEmpty(result.promptId) && result.result?.Urls?.Count > 0)
            result.result.Urls.Insert(0, "超时");

        return Success(new { message = "生成已完成", resultUrl = result.result?.Urls?.Count > 0 ? result.result.Urls[0] : null, promptId = result.promptId });
    }

    [HttpPost("image/character-profile")]
    public async Task<IActionResult> GenerateCharacterProfile([FromBody] AiCharProfileRequest req)
    {
        if (string.IsNullOrEmpty(req.CharacterPrompt))
            return Fail("characterPrompt 不能为空");

        var result = await _proxy.SubmitAndPollAsync<ComfyuiCharProfileOutput>(
            "api/comfyui/zimage/character-profile",
            new
            {
                systemPrompt = req.SystemPrompt,
                characterPrompt = req.CharacterPrompt,
                negativePrompt = req.NegativePrompt,
                width = req.Width ?? 1792,
                height = req.Height ?? 1024
            },
            "zimage-character-profile");

        if (string.IsNullOrEmpty(result.promptId) && result.result?.ImageUrls?.Count == 0)
            return Fail("生成失败");

        return Success(new { resultUrl = result.result?.ImageUrls?.Count > 0 ? result.result.ImageUrls[0] : null, promptId = result.promptId });
    }

    #endregion

    #region Storyboard Generation

    [HttpPost("storyboard/generate")]
    public async Task<IActionResult> GenerateStoryboard([FromBody] AiStoryboardRequest req)
    {
        if (string.IsNullOrEmpty(req.Prompt))
            return Fail("prompt 不能为空");

        var result = await _proxy.SubmitAndPollAsync<ComfyuiStoryboardOutput>(
            "api/comfyui/hidream/storyboard",
            new { prompt = req.Prompt, imagePath = req.ImagePath },
            "hidream-storyboard");

        return Success(new { resultUrl = result.result?.ImageUrls?.Count > 0 ? result.result.ImageUrls[0] : null, promptId = result.promptId });
    }

    #endregion

    #region Video Generation

    [HttpPost("video/text-to-video")]
    public async Task<IActionResult> GenerateTextToVideo([FromBody] AiVideoRequest req)
    {
        if (string.IsNullOrEmpty(req.Prompt))
            return Fail("prompt 不能为空");

        var result = await _proxy.SubmitAndPollAsync<ComfyuiVideoListOutput>(
            "api/comfyui/ltx/text-to-video",
            new
            {
                prompt = req.Prompt,
                width = req.Width ?? 1280,
                height = req.Height ?? 720,
                duration = req.Duration ?? 5,
                fps = req.Fps ?? 25
            },
            "ltx-text-to-video");

        return Success(new { resultUrl = result.result?.Urls?.Count > 0 ? result.result.Urls[0] : null, promptId = result.promptId });
    }

    [HttpPost("video/image-to-video")]
    public async Task<IActionResult> GenerateImageToVideo([FromBody] AiImageToVideoRequest req)
    {
        if (string.IsNullOrEmpty(req.ImagePath) || string.IsNullOrEmpty(req.Prompt))
            return Fail("imagePath 和 prompt 不能为空");

        var result = await _proxy.SubmitAndPollAsync<ComfyuiVideoListOutput>(
            "api/comfyui/ltx/image-to-video",
            new
            {
                imagePath = req.ImagePath,
                prompt = req.Prompt,
                width = req.Width ?? 1280,
                height = req.Height ?? 720,
                duration = req.Duration ?? 3,
                fps = req.Fps ?? 25
            },
            "ltx-image-to-video");

        return Success(new { resultUrl = result.result?.Urls?.Count > 0 ? result.result.Urls[0] : null, promptId = result.promptId });
    }

    #endregion

    #region BGM / Music Generation

    [HttpPost("bgm/generate")]
    public async Task<IActionResult> GenerateBgm([FromBody] AiBgmRequest req)
    {
        if (string.IsNullOrEmpty(req.Prompt))
            return Fail("prompt 不能为空");

        var result = await _proxy.SubmitAndPollAsync<ComfyuiAudioListOutput>(
            "api/comfyui/stable-bgm/generate",
            new { prompt = req.Prompt, duration = req.Duration ?? 150 },
            "stable-bgm-generate");

        return Success(new { resultUrl = result.result?.Urls?.Count > 0 ? result.result.Urls[0] : null, promptId = result.promptId });
    }

    [HttpPost("music/compose")]
    public async Task<IActionResult> ComposeAceMusic([FromBody] AiMusicComposeRequest req)
    {
        if (string.IsNullOrEmpty(req.Prompt) || string.IsNullOrEmpty(req.Lyrics))
            return Fail("prompt 和 lyrics 不能为空");

        var result = await _proxy.SubmitAndPollAsync<ComfyuiAudioListOutput>(
            "api/comfyui/ace-music/compose",
            new
            {
                prompt = req.Prompt,
                lyrics = req.Lyrics,
                seconds = req.Seconds,
                bpm = req.Bpm ?? 88,
                timesignature = req.Timesignature ?? "4",
                language = req.Language ?? "zh",
                keyscale = req.Keyscale ?? "E minor"
            },
            "ace-music-compose");

        return Success(new { resultUrl = result.result?.Urls?.Count > 0 ? result.result.Urls[0] : null, promptId = result.promptId });
    }

    #endregion

    #region Query Result by PromptId (for timeout tasks)

    [HttpGet("result/{promptId}")]
    public async Task<IActionResult> GetResult(string promptId, [FromQuery] string workflowType)
    {
        if (string.IsNullOrEmpty(promptId) || string.IsNullOrEmpty(workflowType))
            return Fail("promptId 和 workflowType 不能为空");

        // Try each possible output type
        var result = await _proxy.GetResultAsync<ComfyuiImageListOutput>(promptId, workflowType);
        if (result.success)
            return Success(new { imageUrls = result.result?.Urls ?? new List<string>(), videoUrls = Array.Empty<string>(), audioUrls = Array.Empty<string>(), text = result.result?.Urls?.Count == 0 ? string.Empty : null });

        var videoResult = await _proxy.GetResultAsync<ComfyuiVideoListOutput>(promptId, workflowType);
        if (videoResult.success)
            return Success(new { imageUrls = Array.Empty<string>(), videoUrls = videoResult.result?.Urls ?? new List<string>(), audioUrls = Array.Empty<string>(), text = string.Empty });

        var audioResult = await _proxy.GetResultAsync<ComfyuiAudioListOutput>(promptId, workflowType);
        if (audioResult.success)
            return Success(new { imageUrls = Array.Empty<string>(), videoUrls = Array.Empty<string>(), audioUrls = audioResult.result?.Urls ?? new List<string>(), text = string.Empty });

        var textResult = await _proxy.GetResultAsync<ComfyuiTextOutput>(promptId, workflowType);
        if (textResult.success)
            return Success(new { imageUrls = Array.Empty<string>(), videoUrls = Array.Empty<string>(), audioUrls = Array.Empty<string>(), text = textResult.result?.Text ?? string.Empty });

        var sbResult = await _proxy.GetResultAsync<ComfyuiStoryboardOutput>(promptId, workflowType);
        if (sbResult.success)
            return Success(new { imageUrls = sbResult.result?.ImageUrls ?? new List<string>(), videoUrls = Array.Empty<string>(), audioUrls = Array.Empty<string>(), text = string.Empty });

        var charResult = await _proxy.GetResultAsync<ComfyuiCharProfileOutput>(promptId, workflowType);
        if (charResult.success)
            return Success(new { imageUrls = charResult.result?.ImageUrls ?? new List<string>(), videoUrls = Array.Empty<string>(), audioUrls = Array.Empty<string>(), text = string.Empty });

        return Fail(result.message ?? "未找到结果，任务可能还在执行中");
    }

    #endregion
}

#region Request DTOs

public class AiImageRequest
{
    public string Prompt { get; set; } = "";
    public string? NegativePrompt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? Seed { get; set; }
}

public class AiCharProfileRequest
{
    public string SystemPrompt { get; set; } = "";
    public string CharacterPrompt { get; set; } = "";
    public string NegativePrompt { get; set; } = "";
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class AiStoryboardRequest
{
    public string Prompt { get; set; } = "";
    public string? ImagePath { get; set; }
}

public class AiVideoRequest
{
    public string Prompt { get; set; } = "";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public int? Fps { get; set; }
}

public class AiImageToVideoRequest
{
    public string ImagePath { get; set; } = "";
    public string Prompt { get; set; } = "";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public int? Fps { get; set; }
}

public class AiBgmRequest
{
    public string Prompt { get; set; } = "";
    public float? Duration { get; set; }
}

public class AiMusicComposeRequest
{
    public string Prompt { get; set; } = "";
    public string Lyrics { get; set; } = "";
    public int? Bpm { get; set; }
    public string? Timesignature { get; set; }
    public string? Language { get; set; }
    public string? Keyscale { get; set; }
    public double? Seconds { get; set; }
}

#endregion
