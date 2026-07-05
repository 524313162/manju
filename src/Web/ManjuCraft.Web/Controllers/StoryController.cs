using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.AI;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

public class StoryController : Controller
{
    private readonly IStoryService _storyService;
    private readonly IProjectService _projectService;
    private readonly IProjectDbContext _dbContext;
    private readonly IAiClientRegistry _registry;

    public StoryController(IStoryService storyService, IProjectService projectService, IProjectDbContext dbContext, IAiClientRegistry registry)
    {
        _storyService = storyService;
        _projectService = projectService;
        _dbContext = dbContext;
        _registry = registry;
    }

    public async Task<IActionResult> Index(long projectId)
    {
        ViewData["Title"] = "剧本创作";
        ViewBag.HideFooter = true;
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return RedirectToAction("Index", "Projects");

        var stories = await _storyService.GetByProjectIdAsync(projectId);
        Story story;
        if (stories.Count == 0)
        {
            story = new Story { ProjectId = projectId, Title = project.Name };
            await _storyService.CreateAsync(story);
            stories = new List<Story> { story };
        }
        else
        {
            story = stories.FirstOrDefault();
        }

        var textProvider = await _dbContext.ApiProviders
            .Where(p => p.Capability == AiCapability.TextToText)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();
        ViewBag.TextProvider = textProvider != null ? new { textProvider.Name, textProvider.Model, textProvider.ApiUrl } : null;

        ViewBag.Project = project;
        ViewBag.Story = story;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateStory(long projectId, string title)
    {
        var story = new Story { ProjectId = projectId, Title = title ?? "默认剧本" };
        await _storyService.CreateAsync(story);
        return Json(new { success = true, data = story });
    }

    [HttpGet]
    public async Task<IActionResult> GetChapters(long storyId)
    {
        var story = await _storyService.GetByIdAsync(storyId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var chapters = await _dbContext.StoryChapters
            .Where(c => c.StoryId == storyId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.ChapterNumber, c.ChapterName, c.Content, c.SortOrder })
            .ToListAsync();

        return Json(new { success = true, data = chapters });
    }

    [HttpPost]
    public async Task<IActionResult> AddChapter([FromBody] ChapterCreateRequest req)
    {
        var story = await _storyService.GetByIdAsync(req.StoryId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var maxOrder = await _dbContext.StoryChapters
            .Where(c => c.StoryId == req.StoryId)
            .MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var chapter = new StoryChapter
        {
            StoryId = req.StoryId,
            ChapterNumber = maxOrder + 1,
            ChapterName = req.ChapterName,
            Content = req.Content ?? "",
            SortOrder = maxOrder + 1
        };

        await _dbContext.StoryChapters.AddAsync(chapter);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, data = new { chapter.Id, chapter.ChapterNumber, chapter.ChapterName, chapter.Content, chapter.SortOrder } });
    }

    [HttpPost]
    public async Task<IActionResult> EditChapter([FromBody] ChapterEditRequest req)
    {
        var existing = await _dbContext.StoryChapters.FindAsync(req.Id);
        if (existing == null) return Json(new { success = false, message = "章节不存在" });

        existing.ChapterName = req.ChapterName;
        existing.Content = req.Content;

        await _dbContext.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteChapter([FromBody] ChapterDeleteRequest req)
    {
        var chapter = await _dbContext.StoryChapters.FindAsync(req.Id);
        if (chapter == null) return Json(new { success = false, message = "章节不存在" });

        _dbContext.StoryChapters.Remove(chapter);

        var remaining = await _dbContext.StoryChapters
            .Where(c => c.StoryId == chapter.StoryId && c.SortOrder > chapter.SortOrder)
            .ToListAsync();

        foreach (var c in remaining)
        {
            c.SortOrder--;
            c.ChapterNumber--;
        }

        await _dbContext.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> BulkAddChapters([FromBody] List<ChapterCreateRequest> chapters)
    {
        if (chapters == null || chapters.Count == 0)
            return Json(new { success = false, message = "章节数据为空" });

        var story = await _storyService.GetByIdAsync(chapters[0].StoryId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var maxOrder = await _dbContext.StoryChapters
            .Where(c => c.StoryId == chapters[0].StoryId)
            .MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var storyChapters = chapters.Select((c, i) => new StoryChapter
        {
            StoryId = chapters[0].StoryId,
            ChapterNumber = maxOrder + i + 1,
            ChapterName = c.ChapterName,
            Content = c.Content,
            SortOrder = maxOrder + i + 1
        }).ToList();

        await _dbContext.StoryChapters.AddRangeAsync(storyChapters);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, data = storyChapters });
    }

    [HttpGet]
    public async Task<IActionResult> LoadChaptersForProduction(long projectId)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        if (stories.Count == 0)
            return Json(new { success = true, data = new List<object>(), storyId = 0L });

        var story = stories[0];
        var chapters = await _dbContext.StoryChapters
            .Where(c => c.StoryId == story.Id)
            .OrderBy(c => c.SortOrder)
            .Select(c => new {
                c.Id,
                c.StoryId,
                c.ChapterNumber,
                c.ChapterName,
                c.Content,
                c.SortOrder
            })
            .ToListAsync();

        return Json(new {
            success = true,
            storyId = story.Id,
            storyTitle = story.Title,
            data = chapters
        });
    }

    public class ChapterCreateRequest
    {
        public long StoryId { get; set; }
        public string ChapterName { get; set; }
        public string Content { get; set; }
    }

    public class ChapterEditRequest
    {
        public long Id { get; set; }
        public string ChapterName { get; set; }
        public string Content { get; set; }
    }

    public class ChapterDeleteRequest
    {
        public long Id { get; set; }
    }
}
