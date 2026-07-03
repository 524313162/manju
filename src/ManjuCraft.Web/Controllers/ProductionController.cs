using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Web.Controllers;

public class ProductionController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IStoryService _storyService;
    private readonly IEpisodeService _episodeService;
    private readonly IShotService _shotService;

    public ProductionController(
        IProjectService projectService,
        IStoryService storyService,
        IEpisodeService episodeService,
        IShotService shotService)
    {
        _projectService = projectService;
        _storyService = storyService;
        _episodeService = episodeService;
        _shotService = shotService;
    }

    public async Task<IActionResult> Index(long projectId)
    {
        ViewData["Title"] = "漫剧创作";
        ViewBag.HideFooter = true;
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return RedirectToAction("Index", "Projects");

        ViewBag.Project = project;
        return View();
    }
}