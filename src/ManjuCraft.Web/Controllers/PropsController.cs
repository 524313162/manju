using ManjuCraft.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;
using ManjuCraft.Application.Service;

namespace ManjuCraft.Web.Controllers;

// MVC route for Props pages
[Route("Props")]
[Route("Props/{action=Index}")]
public class PropsViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Props/Index.cshtml");
    }
}

[Route("api/v1")]
[ApiController]
public class PropsController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;
    private readonly IDeepSeekService _deepSeek;
    private readonly ILogger<PropsController> _logger;

    public PropsController(ProjectDbContext dbContext, IDeepSeekService deepSeek, ILogger<PropsController> logger)
    {
        _dbContext = dbContext;
        _deepSeek = deepSeek;
        _logger = logger;
    }

    [HttpGet("projects/{projectId}/props")]
    public IActionResult List(long projectId)
    {
        var props = _dbContext.Props
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Order)
            .Select(p => new { p.Id, p.Name, p.TwoViewPrompt, p.DefaultWorkflowType, p.Order })
            .ToList();
        return Ok(new { success = true, data = props });
    }

    [HttpPost("projects/{projectId}/props")]
    public IActionResult Create(long projectId, [FromBody] PropDto dto)
    {
        var prop = new Prop
        {
            ProjectId = projectId,
            Name = dto.Name,
            TwoViewPrompt = dto.TwoViewPrompt,
            DefaultWorkflowType = dto.DefaultWorkflowType,
            Order = dto.Order,
            CreatedTime = DateTime.UtcNow.Ticks,
            UpdatedTime = DateTime.UtcNow.Ticks
        };
        _dbContext.Props.Add(prop);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { prop.Id, prop.Name } });
    }

    [HttpPut("props/{id}")]
    public IActionResult Update(long id, [FromBody] PropDto dto)
    {
        var prop = _dbContext.Props.Find(id);
        if (prop == null) return NotFound();
        prop.Name = dto.Name;
        prop.TwoViewPrompt = dto.TwoViewPrompt;
        prop.DefaultWorkflowType = dto.DefaultWorkflowType;
        prop.Order = dto.Order;
        prop.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("props/{id}")]
    public IActionResult Delete(long id)
    {
        var prop = _dbContext.Props.Find(id);
        if (prop == null) return NotFound();
        _dbContext.Props.Remove(prop);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPost("props/{id}/generate-prompt")]
    public async Task<IActionResult> GeneratePrompt(long id, [FromBody] PromptRequest request)
    {
        var prop = _dbContext.Props.Find(id);
        if (prop == null) return NotFound();

        var systemPrompt = "你是一个专业的AI绘画提示词生成助手。请根据道具描述，生成适合AI绘画的双视图提示词（正面、侧面），要求精细建模，风格一致。只输出提示词，不要输出其他内容。";
        
        var userContent = $"请为以下道具生成双视图AI绘画提示词：\n道具名称：{prop.Name}\n道具描述：{request.Description}";
        
        var prompt = await _deepSeek.GenerateAsync(systemPrompt, userContent);
        
        prop.TwoViewPrompt = prompt;
        prop.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        
        return Ok(new { success = true, data = new { prompt } });
    }

    public class PropDto
    {
        public string Name { get; set; } = "";
        public string TwoViewPrompt { get; set; } = "";
        public string DefaultWorkflowType { get; set; } = "Txt2Img";
        public int Order { get; set; }
    }

    public class PromptRequest
    {
        public string Description { get; set; } = "";
    }
}
