using ManjuCraft.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

// MVC route for Skills pages
[Route("Skills")]
[Route("Skills/{action=Index}")]
public class SkillsViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Skills/Index.cshtml");
    }
}

[Route("api/v1")]
[ApiController]
public class SkillsController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;

    public SkillsController(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("projects/{projectId}/skills")]
    public IActionResult List(long projectId)
    {
        var skills = _dbContext.Skills
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Order)
            .Select(s => new { s.Id, s.Name, s.Prompt, s.DefaultWorkflowType, s.Order })
            .ToList();
        return Ok(new { success = true, data = skills });
    }

    [HttpPost("projects/{projectId}/skills")]
    public IActionResult Create(long projectId, [FromBody] SkillDto dto)
    {
        var skill = new Skill
        {
            ProjectId = projectId,
            Name = dto.Name,
            Prompt = dto.Prompt,
            DefaultWorkflowType = dto.DefaultWorkflowType,
            Order = dto.Order,
            CreatedTime = DateTime.UtcNow.Ticks,
            UpdatedTime = DateTime.UtcNow.Ticks
        };
        _dbContext.Skills.Add(skill);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { skill.Id, skill.Name } });
    }

    [HttpPut("skills/{id}")]
    public IActionResult Update(long id, [FromBody] SkillDto dto)
    {
        var skill = _dbContext.Skills.Find(id);
        if (skill == null) return NotFound();
        skill.Name = dto.Name;
        skill.Prompt = dto.Prompt;
        skill.DefaultWorkflowType = dto.DefaultWorkflowType;
        skill.Order = dto.Order;
        skill.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("skills/{id}")]
    public IActionResult Delete(long id)
    {
        var skill = _dbContext.Skills.Find(id);
        if (skill == null) return NotFound();
        _dbContext.Skills.Remove(skill);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    public class SkillDto
    {
        public string Name { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string DefaultWorkflowType { get; set; } = "Txt2Img";
        public int Order { get; set; }
    }
}
