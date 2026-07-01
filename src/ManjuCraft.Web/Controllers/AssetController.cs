using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Infrastructure;
using ManjuCraft.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ManjuCraft.Web.Controllers;

[Route("api/v1/assets")]
[ApiController]
public class AssetController : ControllerBase
{
    private readonly ILogger<AssetController> _logger;
    private readonly ProjectDbContext _dbContext;

    public AssetController(ILogger<AssetController> logger, ProjectDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet("{projectId}/actors")]
    public IActionResult GetActors(long projectId)
    {
        var actors = _dbContext.Actors.Where(a => a.ProjectId == projectId).OrderBy(a => a.Order).ToList();
        return Ok(new { success = true, data = actors });
    }

    [HttpGet("{projectId}/props")]
    public IActionResult GetProps(long projectId)
    {
        var props = _dbContext.Props.Where(p => p.ProjectId == projectId).OrderBy(p => p.Order).ToList();
        return Ok(new { success = true, data = props });
    }

    [HttpGet("{projectId}/scenes")]
    public IActionResult GetScenes(long projectId)
    {
        var scenes = _dbContext.Scenes.Where(s => s.ProjectId == projectId).OrderBy(s => s.Order).ToList();
        return Ok(new { success = true, data = scenes });
    }

    [HttpGet("{projectId}/skills")]
    public IActionResult GetSkills(long projectId)
    {
        var skills = _dbContext.Skills.Where(s => s.ProjectId == projectId).OrderBy(s => s.Order).ToList();
        return Ok(new { success = true, data = skills });
    }

    [HttpGet("{projectId}/bgms")]
    public IActionResult GetBgms(long projectId)
    {
        var bgms = _dbContext.Bgms.Where(b => b.ProjectId == projectId).OrderBy(b => b.Order).ToList();
        return Ok(new { success = true, data = bgms });
    }

    [HttpPut("actors/reorder")]
    public IActionResult ReorderActors([FromBody] ReorderRequest request)
    {
        for (int i = 0; i < request.Ids.Length; i++)
        {
            var actor = _dbContext.Actors.Find(request.Ids[i]);
            if (actor != null)
            {
                actor.Order = i;
                actor.UpdatedTime = DateTime.UtcNow.Ticks;
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    [HttpPut("props/reorder")]
    public IActionResult ReorderProps([FromBody] ReorderRequest request)
    {
        for (int i = 0; i < request.Ids.Length; i++)
        {
            var prop = _dbContext.Props.Find(request.Ids[i]);
            if (prop != null)
            {
                prop.Order = i;
                prop.UpdatedTime = DateTime.UtcNow.Ticks;
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    [HttpPut("scenes/reorder")]
    public IActionResult ReorderScenes([FromBody] ReorderRequest request)
    {
        for (int i = 0; i < request.Ids.Length; i++)
        {
            var scene = _dbContext.Scenes.Find(request.Ids[i]);
            if (scene != null)
            {
                scene.Order = i;
                scene.UpdatedTime = DateTime.UtcNow.Ticks;
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    [HttpPut("skills/reorder")]
    public IActionResult ReorderSkills([FromBody] ReorderRequest request)
    {
        for (int i = 0; i < request.Ids.Length; i++)
        {
            var skill = _dbContext.Skills.Find(request.Ids[i]);
            if (skill != null)
            {
                skill.Order = i;
                skill.UpdatedTime = DateTime.UtcNow.Ticks;
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    [HttpPut("bgms/reorder")]
    public IActionResult ReorderBgms([FromBody] ReorderRequest request)
    {
        for (int i = 0; i < request.Ids.Length; i++)
        {
            var bgm = _dbContext.Bgms.Find(request.Ids[i]);
            if (bgm != null)
            {
                bgm.Order = i;
                bgm.UpdatedTime = DateTime.UtcNow.Ticks;
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    public class ReorderRequest
    {
        public long[] Ids { get; set; } = [];
    }
}
