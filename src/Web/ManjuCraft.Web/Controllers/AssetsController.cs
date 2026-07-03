using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Web.Controllers;

public class AssetsController : Controller
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    public async Task<IActionResult> Index(long? projectId)
    {
        ViewData["Title"] = "资产管理";
        ViewBag.ProjectId = projectId;

        var props = projectId.HasValue ? await _assetService.GetByProjectAsync(projectId.Value, "Prop") : new List<Asset>();
        var scenes = projectId.HasValue ? await _assetService.GetByProjectAsync(projectId.Value, "Scene") : new List<Asset>();
        var bgms = projectId.HasValue ? await _assetService.GetByProjectAsync(projectId.Value, "Bgm") : new List<Asset>();

        ViewBag.Props = props;
        ViewBag.Scenes = scenes;
        ViewBag.Bgms = bgms;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(long projectId, string assetType, string name, string description)
    {
        var asset = new Asset
        {
            ProjectId = projectId,
            AssetType = assetType,
            Name = name,
            Description = description
        };
        await _assetService.CreateAsync(asset);
        return Json(new { success = true, data = asset });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, string name, string description)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return Json(new { success = false, message = "资产不存在" });
        asset.Name = name;
        asset.Description = description;
        await _assetService.UpdateAsync(asset);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        await _assetService.DeleteAsync(id);
        return Json(new { success = true });
    }
}