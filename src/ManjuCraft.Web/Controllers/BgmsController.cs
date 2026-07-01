using ManjuCraft.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

// MVC route for Bgms pages
[Route("Bgms")]
[Route("Bgms/{action=Index}")]
public class BgmsViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Bgms/Index.cshtml");
    }
}

[Route("api/v1")]
[ApiController]
public class BgmsController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;

    public BgmsController(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("projects/{projectId}/bgms")]
    public IActionResult List(long projectId)
    {
        var bgms = _dbContext.Bgms
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.Order)
            .Select(b => new { b.Id, b.Name, b.Prompt, b.DefaultWorkflowType, b.Order })
            .ToList();
        return Ok(new { success = true, data = bgms });
    }

    [HttpPost("projects/{projectId}/bgms")]
    public IActionResult Create(long projectId, [FromBody] BgmDto dto)
    {
        var bgm = new Bgm
        {
            ProjectId = projectId,
            Name = dto.Name,
            Prompt = dto.Prompt,
            DefaultWorkflowType = dto.DefaultWorkflowType,
            Order = dto.Order,
            CreatedTime = DateTime.UtcNow.Ticks,
            UpdatedTime = DateTime.UtcNow.Ticks
        };
        _dbContext.Bgms.Add(bgm);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { bgm.Id, bgm.Name } });
    }

    [HttpPut("bgms/{id}")]
    public IActionResult Update(long id, [FromBody] BgmDto dto)
    {
        var bgm = _dbContext.Bgms.Find(id);
        if (bgm == null) return NotFound();
        bgm.Name = dto.Name;
        bgm.Prompt = dto.Prompt;
        bgm.DefaultWorkflowType = dto.DefaultWorkflowType;
        bgm.Order = dto.Order;
        bgm.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("bgms/{id}")]
    public IActionResult Delete(long id)
    {
        var bgm = _dbContext.Bgms.Find(id);
        if (bgm == null) return NotFound();
        _dbContext.Bgms.Remove(bgm);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPost("bgms/{id}/generate-audio")]
    public IActionResult GenerateAudio(long id, [FromBody] BgmGenerateRequest request)
    {
        var bgm = _dbContext.Bgms.Find(id);
        if (bgm == null) return NotFound();
        bgm.Prompt = request.Prompt;
        bgm.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { taskId = Guid.NewGuid().ToString(), status = "pending", message = "BGM生成任务已提交" } });
    }

    public class BgmDto
    {
        public string Name { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string DefaultWorkflowType { get; set; } = "MusicGen";
        public int Order { get; set; }
    }

    public class BgmGenerateRequest
    {
        public string Prompt { get; set; } = "";
    }
}
