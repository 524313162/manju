using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service;
using ManjuCraft.Infrastructure;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IProjectDbContext _db;

    public ProjectsController(IProjectService projectService, IProjectDbContext db)
    {
        _projectService = projectService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "项目管理";
        var projects = await _projectService.GetAllAsync();
        var projectHasStory = new HashSet<long>(
            await _db.Stories.Select(s => s.ProjectId).Distinct().ToListAsync());
        ViewBag.ProjectHasStory = projectHasStory;
        return View(projects);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "项目名称不能为空" });

        var project = new Project { Name = name.Trim() };
        await _projectService.CreateAsync(project);
        return Json(new { success = true, data = project });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "项目名称不能为空" });

        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
            return Json(new { success = false, message = "项目不存在" });

        project.Name = name.Trim();
        await _projectService.UpdateAsync(project);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        await _projectService.DeleteAsync(id);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        return Json(new { success = true, data = project });
    }
}