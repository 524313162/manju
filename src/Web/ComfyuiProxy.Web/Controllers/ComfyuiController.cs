using Microsoft.AspNetCore.Mvc;
using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.Controllers;

/// <summary>
/// ComfyUI 代理控制器 — 每个 Action 直接构造 Flow 类执行
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ComfyuiController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public ComfyuiController(IHttpClientFactory httpFactory, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _httpClient = httpFactory.CreateClient("http");
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    // ═══ 01. ZIMAGE 文生图 ═══
    [HttpPost("zimage/text-to-image")]
    public async Task<IActionResult> ZImageTextToImage([FromBody] ZImageTextToImageRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt 不能为空" });

        var flow = new Flow_01_TextToImage(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_01_TextToImage>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    // ═══ 02. ZIMAGE 人物档案 ═══
    [HttpPost("zimage/character-profile")]
    public async Task<IActionResult> ZImageCharacterProfile([FromBody] ZImageCharacterProfileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt 不能为空" });

        var flow = new Flow_02_CharacterProfile(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_02_CharacterProfile>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    // ═══ 03. LTX 文生视频 ═══
    [HttpPost("ltx/text-to-video")]
    public async Task<IActionResult> LtxTextToVideo([FromBody] LtxTextToVideoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt 不能为空" });

        var flow = new Flow_03_TextToVideo(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_03_TextToVideo>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    // ═══ 04. LTX 图生视频 ═══
    [HttpPost("ltx/image-to-video")]
    public async Task<IActionResult> LtxImageToVideo([FromBody] LtxImageToVideoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt 不能为空" });

        var flow = new Flow_04_ImageToVideo(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_04_ImageToVideo>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    // ═══ 05. HIDREAM 分镜 ═══
    [HttpPost("hidream/storyboard")]
    public async Task<IActionResult> HiDreamStoryboard([FromBody] HiDreamStoryboardRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt 不能为空" });

        var flow = new Flow_05_Storyboard(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_05_Storyboard>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    // ═══ 06. ACE-MUSIC ═══
    [HttpPost("ace/music")]
    public async Task<IActionResult> AceMusic([FromBody] AceMusicRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Tags))
            return BadRequest(new { error = "tags 不能为空" });

        var flow = new Flow_06_MusicCompose(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_06_MusicCompose>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    // ═══ 07. STABLE-BGM ═══
    [HttpPost("stable/bgm")]
    public async Task<IActionResult> StableBgm([FromBody] StableBgmRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt 不能为空" });

        var flow = new Flow_07_BgmGenerate(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_07_BgmGenerate>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    // ═══ 08. LLM-QWen ═══
    [HttpPost("llm/qwen")]
    public async Task<IActionResult> LlmQwen([FromBody] LlmQwenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt 不能为空" });

        var flow = new Flow_08_LlmScript(_httpClient, _configuration,
            _loggerFactory.CreateLogger<Flow_08_LlmScript>());
        var result = await flow.ExecuteAsync(req);
        return ToResponse(result);
    }

    private IActionResult ToResponse(WorkflowExecuteResponse result)
    {
        if (!result.Success)
            return StatusCode(500, new { error = result.Error, promptId = result.PromptId });
        return Ok(result);
    }
}
