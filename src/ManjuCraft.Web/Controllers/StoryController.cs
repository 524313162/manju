using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ManjuCraft.Web.Controllers;

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

[Route("api/v1/story")]
[ApiController]
public class StoryController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly IDeepSeekService _deepSeek;
    private readonly ProjectDbContext _dbContext;
    private readonly ILogger<StoryController> _logger;

    public StoryController(IStoryService storyService, IDeepSeekService deepSeek, ProjectDbContext dbContext, ILogger<StoryController> logger)
    {
        _storyService = storyService;
        _deepSeek = deepSeek;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("{projectId}")]
    public async Task<IActionResult> Get(long projectId)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        var story = stories.FirstOrDefault();
        if (story != null)
        {
            var chapters = await _dbContext.StoryChapters
                .Where(c => c.StoryId == story.Id)
                .OrderBy(c => c.Order)
                .ToListAsync();
            return Ok(new { success = true, data = new { story.Id, story.Title, story.ProjectId, story.CreatedTime, story.UpdatedTime, Chapters = chapters } });
        }
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPost("{projectId}")]
    public async Task<IActionResult> Create(long projectId, [FromBody] StoryCreateDto dto)
    {
        var story = new Story
        {
            ProjectId = projectId,
            Title = dto.Title,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        story = await _storyService.CreateAsync(story);
        return Ok(new { success = true, data = new { story.Id, story.Title } });
    }

    [HttpPut("{projectId}/{id}")]
    public async Task<IActionResult> Update(long projectId, long id, [FromBody] StoryUpdateDto dto)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        var story = stories.FirstOrDefault(s => s.Id == id);
        if (story == null) return NotFound();

        story.Title = dto.Title;
        story.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _storyService.UpdateAsync(story);
        return Ok(new { success = true, data = story });
    }

    [HttpDelete("{projectId}/{id}")]
    public async Task<IActionResult> Delete(long projectId, long id)
    {
        await _storyService.DeleteAsync(id);
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpGet("{projectId}/chapters")]
    public async Task<IActionResult> GetChapters(long projectId)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        var story = stories.FirstOrDefault();
        if (story == null) return Ok(new { success = true, data = new List<object>() });

        var chapters = await _dbContext.StoryChapters
            .Where(c => c.StoryId == story.Id)
            .OrderBy(c => c.Order)
            .Select(c => new { c.Id, c.StoryId, c.ChapterNumber, c.ChapterName, c.Content, c.Order, c.CreatedTime, c.UpdatedTime })
            .ToListAsync();
        return Ok(new { success = true, data = chapters });
    }

    [HttpPost("{projectId}/chapters")]
    public async Task<IActionResult> CreateChapter(long projectId, [FromBody] ChapterCreateDto dto)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        var story = stories.FirstOrDefault();
        if (story == null) return NotFound(new { success = false, message = "请先创建故事" });

        var chapter = new StoryChapter
        {
            StoryId = story.Id,
            ChapterNumber = dto.ChapterNumber,
            ChapterName = dto.ChapterName,
            Content = dto.Content,
            Order = dto.Order,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _dbContext.StoryChapters.Add(chapter);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, data = new { chapter.Id, chapter.ChapterNumber, chapter.ChapterName } });
    }

    [HttpPut("{projectId}/chapters/{chapterId}")]
    public async Task<IActionResult> UpdateChapter(long projectId, long chapterId, [FromBody] ChapterUpdateDto dto)
    {
        var chapter = await _dbContext.StoryChapters.FindAsync(chapterId);
        if (chapter == null) return NotFound();
        chapter.ChapterNumber = dto.ChapterNumber;
        chapter.ChapterName = dto.ChapterName;
        chapter.Content = dto.Content;
        chapter.Order = dto.Order;
        chapter.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("{projectId}/chapters/{chapterId}")]
    public async Task<IActionResult> DeleteChapter(long projectId, long chapterId)
    {
        var chapter = await _dbContext.StoryChapters.FindAsync(chapterId);
        if (chapter == null) return NotFound();
        _dbContext.StoryChapters.Remove(chapter);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPost("{projectId}/generate")]
    public async Task<IActionResult> GenerateStory(long projectId, [FromBody] StoryGenerateDto dto)
    {
        try
        {
            var systemPrompt = "你是一个专业的漫剧编剧。请根据用户提供的故事主题和设定，生成一个完整的故事大纲，包含标题和各章节内容。";
            var userContent = $"请生成一个漫剧故事：\n主题：{dto.Topic}\n章节数：{dto.ChapterCount}\n风格要求：{dto.Style ?? "漫剧风格"}";

            var result = await _deepSeek.GenerateAsync(systemPrompt, userContent);

            var stories = await _storyService.GetByProjectIdAsync(projectId);
            var story = stories.FirstOrDefault();
            if (story == null)
            {
                story = new Story
                {
                    ProjectId = projectId,
                    Title = dto.Topic,
                    CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                story = await _storyService.CreateAsync(story);
            }

            for (int i = 1; i <= dto.ChapterCount; i++)
            {
                var chapter = new StoryChapter
                {
                    StoryId = story.Id,
                    ChapterNumber = i.ToString(),
                    ChapterName = $"第{i}章",
                    Content = result,
                    Order = i,
                    CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                _dbContext.StoryChapters.Add(chapter);
            }
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, data = new { storyId = story.Id, title = story.Title, generatedContent = result } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "故事生成失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{projectId}/chapters/generate")]
    public async Task<IActionResult> GenerateChapterContent(long projectId, [FromBody] ChapterGenerateDto dto)
    {
        try
        {
            var stories = await _storyService.GetByProjectIdAsync(projectId);
            var story = stories.FirstOrDefault();
            if (story == null) return NotFound(new { success = false, message = "请先创建故事" });

            var systemPrompt = "你是一个专业的漫剧编剧。请根据故事标题和章节要求，生成详细的章节内容。";
            var userContent = $"故事标题：{story.Title}\n章节名称：{dto.ChapterName}\n内容要求：{dto.Description}";
            var content = await _deepSeek.GenerateAsync(systemPrompt, userContent);

            return Ok(new { success = true, data = new { content } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "章节内容生成失败");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    public class StoryCreateDto
    {
        public string Title { get; set; } = "";
    }

    public class StoryUpdateDto
    {
        public string Title { get; set; } = "";
    }

    public class ChapterCreateDto
    {
        public string ChapterNumber { get; set; } = "";
        public string ChapterName { get; set; } = "";
        public string Content { get; set; } = "";
        public int Order { get; set; }
    }

    public class ChapterUpdateDto
    {
        public string ChapterNumber { get; set; } = "";
        public string ChapterName { get; set; } = "";
        public string Content { get; set; } = "";
        public int Order { get; set; }
    }

    public class StoryGenerateDto
    {
        public string Topic { get; set; } = "";
        public int ChapterCount { get; set; } = 3;
        public string? Style { get; set; }
    }

    public class ChapterGenerateDto
    {
        public string ChapterName { get; set; } = "";
        public string Description { get; set; } = "";
    }
}