using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Web.Controllers;

public class ActorsController : Controller
{
    private readonly IAssetService _assetService;

    public ActorsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    public async Task<IActionResult> Index(long? projectId)
    {
        ViewData["Title"] = "角色管理";
        ViewBag.HideFooter = true;
        ViewBag.ProjectId = projectId;
        var actors = projectId.HasValue
            ? await _assetService.GetByProjectAsync(projectId.Value, "Actor")
            : new List<Asset>();
        return View(actors);
    }

    [HttpPost]
    public async Task<IActionResult> Create(long projectId, string name, string description)
    {
        var actor = new Asset
        {
            ProjectId = projectId,
            AssetType = "Actor",
            Name = name,
            Description = description
        };
        await _assetService.CreateAsync(actor);
        return Json(new { success = true, data = actor });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, string name, string description)
    {
        var actor = await _assetService.GetByIdAsync(id);
        if (actor == null) return Json(new { success = false, message = "角色不存在" });
        actor.Name = name;
        actor.Description = description;
        await _assetService.UpdateAsync(actor);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        await _assetService.DeleteAsync(id);
        return Json(new { success = true });
    }
}