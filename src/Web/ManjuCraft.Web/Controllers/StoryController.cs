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
        var story = stories.FirstOrDefault();

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

    [HttpPost]
    public async Task<IActionResult> AddChapter(long storyId, string chapterName, string chapterNumber, string content)
    {
        var stories = await _storyService.GetByProjectIdAsync(0);
        var story = await _storyService.GetByIdAsync(storyId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var chapter = new StoryChapter
        {
            StoryId = storyId,
            ChapterNumber = chapterNumber ?? "第1章",
            ChapterName = chapterName,
            Content = content ?? "",
            Order = 0
        };

        return Json(new { success = true, data = chapter });
    }

    [HttpPost]
    public async Task<IActionResult> SaveChapter(long storyId, string chapterName, string chapterNumber, string content)
    {
        var story = await _storyService.GetByIdAsync(storyId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var existing = await _dbContext.StoryChapters
            .Where(c => c.StoryId == storyId)
            .OrderByDescending(c => c.Order)
            .FirstOrDefaultAsync();

        var order = existing != null ? existing.Order + 1 : 0;

        var chapter = new StoryChapter
        {
            StoryId = storyId,
            ChapterNumber = chapterNumber ?? "第1章",
            ChapterName = chapterName,
            Content = content ?? "",
            Order = order
        };

        await _dbContext.StoryChapters.AddAsync(chapter);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, data = chapter });
    }

    [HttpPost]
    public async Task<IActionResult> BulkAddChapters([FromBody] List<ChapterCreateRequest> chapters)
    {
        if (chapters == null || chapters.Count == 0)
            return Json(new { success = false, message = "章节数据为空" });

        var story = await _storyService.GetByIdAsync(chapters[0].StoryId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var existing = await _dbContext.StoryChapters
            .Where(c => c.StoryId == chapters[0].StoryId)
            .OrderByDescending(c => c.Order)
            .FirstOrDefaultAsync();

        var order = existing != null ? existing.Order + 1 : 0;

        var storyChapters = chapters.Select((c, i) => new StoryChapter
        {
            StoryId = chapters[0].StoryId,
            ChapterNumber = c.ChapterNumber,
            ChapterName = c.ChapterName,
            Content = c.Content,
            Order = order + i
        }).ToList();

        await _dbContext.StoryChapters.AddRangeAsync(storyChapters);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, data = storyChapters });
    }

    public class ChapterCreateRequest
    {
        public long StoryId { get; set; }
        public string ChapterNumber { get; set; }
        public string ChapterName { get; set; }
        public string Content { get; set; }
    }
}