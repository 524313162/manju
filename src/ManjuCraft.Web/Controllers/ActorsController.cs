using ManjuCraft.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;
using ManjuCraft.Application.Service;

namespace ManjuCraft.Web.Controllers;

// MVC route for Actors pages
[Route("Actors")]
[Route("Actors/{action=Index}")]
public class ActorsViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Actors/Index.cshtml");
    }
}

[Route("api/v1")]
[ApiController]
public class ActorsController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;
    private readonly IDeepSeekService _deepSeek;
    private readonly ILogger<ActorsController> _logger;

    public ActorsController(ProjectDbContext dbContext, IDeepSeekService deepSeek, ILogger<ActorsController> logger)
    {
        _dbContext = dbContext;
        _deepSeek = deepSeek;
        _logger = logger;
    }

    [HttpGet("projects/{projectId}/actors")]
    public IActionResult List(long projectId)
    {
        var actors = _dbContext.Actors
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.Order)
            .Select(a => new { a.Id, a.Name, a.Description, a.FourViewPrompt, a.DefaultWorkflowType, a.Order })
            .ToList();
        return Ok(new { success = true, data = actors });
    }

    [HttpPost("projects/{projectId}/actors")]
    public IActionResult Create(long projectId, [FromBody] ActorDto dto)
    {
        var actor = new Actor
        {
            ProjectId = projectId,
            Name = dto.Name,
            Description = dto.Description,
            FourViewPrompt = dto.FourViewPrompt,
            DefaultWorkflowType = dto.DefaultWorkflowType,
            Order = dto.Order,
            CreatedTime = DateTime.UtcNow.Ticks,
            UpdatedTime = DateTime.UtcNow.Ticks
        };
        _dbContext.Actors.Add(actor);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { actor.Id, actor.Name } });
    }

    [HttpPut("actors/{id}")]
    public IActionResult Update(long id, [FromBody] ActorDto dto)
    {
        var actor = _dbContext.Actors.Find(id);
        if (actor == null) return NotFound();
        actor.Name = dto.Name;
        actor.Description = dto.Description;
        actor.FourViewPrompt = dto.FourViewPrompt;
        actor.DefaultWorkflowType = dto.DefaultWorkflowType;
        actor.Order = dto.Order;
        actor.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("actors/{id}")]
    public IActionResult Delete(long id)
    {
        var actor = _dbContext.Actors.Find(id);
        if (actor == null) return NotFound();
        _dbContext.Actors.Remove(actor);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPost("actors/{id}/generate-prompt")]
    public async Task<IActionResult> GeneratePrompt(long id, [FromBody] PromptRequest request)
    {
        var actor = _dbContext.Actors.Find(id);
        if (actor == null) return NotFound();

        var systemPrompt = "你是一个专业的AI绘画提示词生成助手。请根据角色描述，生成适合AI绘画的四视图提示词（正面半身、背面、侧面、三视角），风格为漫剧风格。提示词应包含角色特征、服装、姿势、表情等细节。只输出提示词，不要输出其他内容。";
        
        var userContent = $"请为以下角色生成四视图AI绘画提示词：\n角色名称：{actor.Name}\n角色描述：{request.Description}";
        
        var prompt = await _deepSeek.GenerateAsync(systemPrompt, userContent);
        
        actor.FourViewPrompt = prompt;
        actor.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        
        return Ok(new { success = true, data = new { prompt } });
    }

    [HttpGet("actors/{id}/images")]
    public IActionResult GetImages(long id)
    {
        var images = _dbContext.EntityImages
            .Where(e => e.EntityType == "Actor" && e.EntityId == id)
            .ToList();
        return Ok(new { success = true, data = images });
    }

    public class ActorDto
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string FourViewPrompt { get; set; } = "";
        public string DefaultWorkflowType { get; set; } = "Txt2Img";
        public int Order { get; set; }
    }

    public class PromptRequest
    {
        public string Description { get; set; } = "";
    }
}
