using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

// MVC route for Story pages — does NOT use [ApiController] or [ApiController()] so it returns Razor Views
[Route("Story")]
[Route("Story/{action=Index}")]
public class StoryViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Story/Index.cshtml");
    }
}

[Route("api/v1/projects/{projectId}/story")]
[ApiController]
public class StoryController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly IDeepSeekService _deepSeek;
    private readonly ILogger<StoryController> _logger;

    public StoryController(IStoryService storyService, IDeepSeekService deepSeek, ILogger<StoryController> logger)
    {
        _storyService = storyService;
        _deepSeek = deepSeek;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(long projectId)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        var story = stories.FirstOrDefault();
        return Ok(new { success = true, data = story });
    }

    [HttpPut]
    public async Task<IActionResult> Update(long projectId, [FromBody] StoryUpdateDto dto)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        Story story;
        if (stories.Any())
        {
            story = stories.First();
            story.Content = dto.Content;
            story = await _storyService.UpdateAsync(story);
        }
        else
        {
            story = new Domain.Models.Story { ProjectId = projectId, Content = dto.Content };
            story = await _storyService.CreateAsync(story);
        }
        return Ok(new { success = true, data = story });
    }

    [HttpPost("split")]
    public async Task<IActionResult> Split(long projectId, [FromBody] StorySplitDto dto)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        var story = stories.FirstOrDefault();
        if (story != null)
        {
            var systemPrompt = "你是一个专业的漫剧导演和编剧助手。请将用户提供的故事内容拆分成清晰的漫剧分镜片段。每个分镜片段应包含：场景编号、场景描述、角色对话（如有）、镜头建议。请以清晰的列表格式输出，每个片段之间用空行分隔。";
            var userContent = $"请拆分以下故事内容为漫剧分镜片段：\n\n{dto.Content}";
            var splitResult = await _deepSeek.GenerateAsync(systemPrompt, userContent);
            story = await _storyService.SplitAsync(story.Id, splitResult);
            return Ok(new { success = true, data = story });
        }
        return Ok(new { success = true, data = (object)new { id = projectId, splitContent = "" } });
    }

    public class StoryUpdateDto
    {
        public string Content { get; set; } = "";
    }

    public class StorySplitDto
    {
        public string Content { get; set; } = "";
    }
}
