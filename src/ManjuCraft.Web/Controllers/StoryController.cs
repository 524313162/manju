using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Web.Controllers;

public class StoryController : Controller
{
    private readonly IStoryService _storyService;
    private readonly IProjectService _projectService;

    public StoryController(IStoryService storyService, IProjectService projectService)
    {
        _storyService = storyService;
        _projectService = projectService;
    }

    public async Task<IActionResult> Index(long projectId)
    {
        ViewData["Title"] = "剧本创作";
        ViewBag.HideFooter = true;
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return RedirectToAction("Index", "Projects");

        var stories = await _storyService.GetByProjectIdAsync(projectId);
        var story = stories.FirstOrDefault();
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
}