using ManjuCraft.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;
using ManjuCraft.Application.Service;

namespace ManjuCraft.Web.Controllers;

// MVC route for Scenes pages
[Route("Scenes")]
[Route("Scenes/{action=Index}")]
public class ScenesViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Scenes/Index.cshtml");
    }
}

[Route("api/v1")]
[ApiController]
public class ScenesController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;
    private readonly IDeepSeekService _deepSeek;
    private readonly ILogger<ScenesController> _logger;

    public ScenesController(ProjectDbContext dbContext, IDeepSeekService deepSeek, ILogger<ScenesController> logger)
    {
        _dbContext = dbContext;
        _deepSeek = deepSeek;
        _logger = logger;
    }

    [HttpGet("projects/{projectId}/scenes")]
    public IActionResult List(long projectId)
    {
        var scenes = _dbContext.Scenes
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Order)
            .Select(s => new { s.Id, s.Name, s.ImagePrompt, s.DefaultWorkflowType, s.Order })
            .ToList();
        return Ok(new { success = true, data = scenes });
    }

    [HttpPost("projects/{projectId}/scenes")]
    public IActionResult Create(long projectId, [FromBody] SceneDto dto)
    {
        var scene = new Scene
        {
            ProjectId = projectId,
            Name = dto.Name,
            ImagePrompt = dto.ImagePrompt,
            DefaultWorkflowType = dto.DefaultWorkflowType,
            Order = dto.Order,
            CreatedTime = DateTime.UtcNow.Ticks,
            UpdatedTime = DateTime.UtcNow.Ticks
        };
        _dbContext.Scenes.Add(scene);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { scene.Id, scene.Name } });
    }

    [HttpPut("scenes/{id}")]
    public IActionResult Update(long id, [FromBody] SceneDto dto)
    {
        var scene = _dbContext.Scenes.Find(id);
        if (scene == null) return NotFound();
        scene.Name = dto.Name;
        scene.ImagePrompt = dto.ImagePrompt;
        scene.DefaultWorkflowType = dto.DefaultWorkflowType;
        scene.Order = dto.Order;
        scene.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("scenes/{id}")]
    public IActionResult Delete(long id)
    {
        var scene = _dbContext.Scenes.Find(id);
        if (scene == null) return NotFound();
        _dbContext.Scenes.Remove(scene);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPost("scenes/{id}/generate-prompt")]
    public async Task<IActionResult> GeneratePrompt(long id, [FromBody] PromptRequest request)
    {
        var scene = _dbContext.Scenes.Find(id);
        if (scene == null) return NotFound();

        var systemPrompt = "你是一个专业的AI绘画提示词生成助手。请根据场景描述，生成适合AI绘画的背景提示词，风格为漫剧背景风格。提示词应包含场景氛围、光线、色彩、景物细节等。只输出提示词，不要输出其他内容。";
        
        var userContent = $"请为以下场景生成AI绘画提示词：\n场景名称：{scene.Name}\n场景描述：{request.Description}";
        
        var prompt = await _deepSeek.GenerateAsync(systemPrompt, userContent);
        
        scene.ImagePrompt = prompt;
        scene.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        
        return Ok(new { success = true, data = new { prompt } });
    }

    public class SceneDto
    {
        public string Name { get; set; } = "";
        public string ImagePrompt { get; set; } = "";
        public string DefaultWorkflowType { get; set; } = "Txt2Img";
        public int Order { get; set; }
    }

    public class PromptRequest
    {
        public string Description { get; set; } = "";
    }
}
