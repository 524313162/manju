using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace ManjuCraft.Web.Controllers;

public class AssetsController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IProjectService _projectService;

    public AssetsController(IAssetService assetService, IProjectService projectService)
    {
        _assetService = assetService;
        _projectService = projectService;
    }

    public async Task<IActionResult> Index(long projectId, string type = "Actor")
    {
        if (projectId <= 0)
        {
            return RedirectToAction("Index", "Projects");
        }

        await LoadProjectsAsync();

        var typeMap = new Dictionary<string, string>
        {
            { "Actor", "角色" },
            { "Scene", "场景" },
            { "Bgm", "BGM" },
            { "Skill", "技能" },
            { "Prop", "道具" }
        };
        var typeName = typeMap.ContainsKey(type) ? typeMap[type] : "角色";

        var assets = await _assetService.GetByProjectAsync(projectId, type);

        ViewBag.ProjectId = projectId;
        ViewBag.Type = type;
        ViewBag.TypeName = typeName;
        ViewBag.Assets = assets;

        try
        {
            var proj = await _projectService.GetByIdAsync(projectId);
            ViewBag.ProjectName = proj?.Name ?? "";
        }
        catch { ViewBag.ProjectName = ""; }

        return View("Index");
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var projects = await _projectService.GetAllAsync();
            ViewBag.Projects = projects;
        }
        catch { ViewBag.Projects = new List<Project>(); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(long? projectId, string? assetType, string? name, string? description, int order = 0)
    {
        if (string.IsNullOrEmpty(name))
            return Content(ToJson(false, "名称不能为空"));

        var asset = new Asset
        {
            ProjectId = projectId ?? 0,
            AssetType = assetType ?? "Actor",
            Name = name,
            Description = description ?? String.Empty,
            Order = order
        };
        await _assetService.CreateAsync(asset);
        return Content(ToJson(true));
    }

    [HttpPost]
    public async Task<IActionResult> Edit()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }
        body = body.Trim();
        if (string.IsNullOrEmpty(body))
            return Content(ToJson(false, "参数错误"));

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0L;
        var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "";
        var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : "";
        var order = root.TryGetProperty("order", out var orderProp) ? orderProp.GetInt32() : 0;

        if (id <= 0)
            return Content(ToJson(false, "参数错误"));

        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null)
            return Content(ToJson(false, "资产不存在"));

        asset.Name = name;
        asset.Description = description;
        asset.Order = order;
        await _assetService.UpdateAsync(asset);
        return Content(ToJson(true));
    }

    [HttpPost]
    public async Task<IActionResult> Delete()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }
        body = body.Trim();
        if (string.IsNullOrEmpty(body))
            return Content(ToJson(false, "参数错误"));

        using var doc = JsonDocument.Parse(body);
        var id = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0L;

        if (id <= 0)
            return Content(ToJson(false, "参数错误"));

        await _assetService.DeleteAsync(id);
        return Content(ToJson(true));
    }

    private string ToJson(bool success, string? message = null)
    {
        var obj = new Dictionary<string, object> { { "success", success } };
        if (!string.IsNullOrEmpty(message))
            obj["message"] = message;
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }
}
