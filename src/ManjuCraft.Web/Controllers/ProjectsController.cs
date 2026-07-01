using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Infrastructure;
using ManjuCraft.Web.ViewModels;

namespace ManjuCraft.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();
        var vm = new ProjectsIndexViewModel { Projects = projects.ToList() };
        ViewData["CurrentProjectId"] = "";
        return View(vm);
    }
}
