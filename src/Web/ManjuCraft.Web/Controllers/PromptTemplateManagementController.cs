using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

[Route("PromptTemplateManagement")]
public class PromptTemplateManagementController : Controller
{
    private readonly IProjectDbContext _db;

    public PromptTemplateManagementController(IProjectDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "提示词管理";
        ViewBag.HideFooter = true;
        ViewBag.Templates = await _db.PromptTemplates.OrderByDescending(p => p.Id).ToListAsync();
        return View();
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] TemplateRequest req)
    {
        var template = new PromptTemplate
        {
            Name = req.Name,
            TemplateType = req.TemplateType,
            Content = req.Content,
            IsDefault = req.IsDefault
        };
        _db.PromptTemplates.Add(template);
        await _db.SaveChangesAsync();
        return Json(new { success = true, data = template });
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit([FromRoute] long id, [FromBody] TemplateRequest req)
    {
        var existing = await _db.PromptTemplates.FindAsync(id);
        if (existing == null) return Json(new { success = false, message = "不存在" });

        existing.Name = req.Name;
        existing.TemplateType = req.TemplateType;
        existing.Content = req.Content;
        existing.IsDefault = req.IsDefault;
        existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromQuery] long id)
    {
        var existing = await _db.PromptTemplates.FindAsync(id);
        if (existing == null) return Json(new { success = false, message = "不存在" });

        _db.PromptTemplates.Remove(existing);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet("detail/{id}")]
    public async Task<IActionResult> Detail(long id)
    {
        var template = await _db.PromptTemplates.FindAsync(id);
        if (template == null) return Json(new { success = false, message = "不存在" });
        return Json(new { success = true, data = template });
    }

    public class TemplateRequest
    {
        public string Name { get; set; }
        public string TemplateType { get; set; }
        public string Content { get; set; }
        public bool IsDefault { get; set; }
    }
}
