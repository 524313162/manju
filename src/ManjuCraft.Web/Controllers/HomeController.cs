using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Web.ViewModels;

namespace ManjuCraft.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IProjectService projectService, ILogger<HomeController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();
        var vm = new ProjectsIndexViewModel { Projects = projects.ToList() };
        return View(vm);
    }

    public IActionResult Error()
    {
        return View();
    }
}
