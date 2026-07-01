using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

// MVC route for Episodes pages
[Route("Episodes")]
[Route("Episodes/{action=Index}")]
public class EpisodesViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        return View("~/Views/Episodes/Index.cshtml");
    }
}

[Route("api/v1/episodes")]
[ApiController]
public class EpisodeCrudController : ControllerBase
{
    private readonly ProjectDbContext _dbContext;

    public EpisodeCrudController(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{id}")]
    public IActionResult GetById(long id)
    {
        var episode = _dbContext.Episodes.Find(id);
        if (episode == null) return NotFound();
        return Ok(new { success = true, data = episode });
    }

    [HttpGet("project/{projectId}")]
    public IActionResult ListByProject(long projectId)
    {
        var episodes = _dbContext.Episodes
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Order)
            .Select(e => new {
                Id = e.Id, Name = e.Name, e.Duration, e.Order,
                ShotCount = _dbContext.Shots.Count(s => s.EpisodeId == e.Id)
            })
            .ToList();
        return Ok(new { success = true, data = episodes });
    }

    [HttpPost("project/{projectId}")]
    public IActionResult Create(long projectId, [FromBody] EpisodeDto dto)
    {
        var episode = new Domain.Models.Episode
        {
            ProjectId = projectId,
            Name = dto.Name,
            Duration = dto.Duration,
            Order = dto.Order,
            CreatedTime = DateTime.UtcNow.Ticks,
            UpdatedTime = DateTime.UtcNow.Ticks
        };
        _dbContext.Episodes.Add(episode);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { episode.Id, episode.Name } });
    }

    [HttpPut("{id}")]
    public IActionResult Update(long id, [FromBody] EpisodeDto dto)
    {
        var episode = _dbContext.Episodes.Find(id);
        if (episode == null) return NotFound();
        episode.Name = dto.Name;
        episode.Duration = dto.Duration;
        episode.Order = dto.Order;
        episode.UpdatedTime = DateTime.UtcNow.Ticks;
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(long id)
    {
        var episode = _dbContext.Episodes.Find(id);
        if (episode == null) return NotFound();
        _dbContext.Episodes.Remove(episode);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpGet("{episodeId}/shots")]
    public IActionResult GetShots(long episodeId)
    {
        var shots = _dbContext.Shots
            .Where(s => s.EpisodeId == episodeId)
            .OrderBy(s => s.Order)
            .Select(s => new {
                s.Id, s.EpisodeId, s.FirstFramePrompt, s.FirstFrameWorkflowType,
                s.Dialog, s.VideoPrompt, s.VideoWorkflowType, s.Order,
                s.CreatedTime, s.UpdatedTime
            })
            .ToList();
        return Ok(new { success = true, data = shots });
    }

    [HttpPost("{episodeId}/shots")]
    public IActionResult CreateShot(long episodeId, [FromBody] ShotDto dto)
    {
        var shot = new Domain.Models.Shot
        {
            EpisodeId = episodeId,
            FirstFramePrompt = dto.FirstFramePrompt,
            FirstFrameWorkflowType = dto.FirstFrameWorkflowType ?? "Img2Img",
            Dialog = dto.Dialog,
            VideoPrompt = dto.VideoPrompt,
            VideoWorkflowType = dto.VideoWorkflowType ?? "Img2Video",
            Order = dto.Order,
            CreatedTime = DateTime.UtcNow.Ticks,
            UpdatedTime = DateTime.UtcNow.Ticks
        };
        _dbContext.Shots.Add(shot);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { shot.Id, shot.EpisodeId } });
    }

    [HttpPut("reorder")]
    public IActionResult Reorder([FromBody] ReorderEpisodesDto dto)
    {
        for (int i = 0; i < dto.EpisodeIds.Length; i++)
        {
            var ep = _dbContext.Episodes.Find(dto.EpisodeIds[i]);
            if (ep != null)
            {
                ep.Order = i;
                ep.UpdatedTime = DateTime.UtcNow.Ticks;
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    [HttpPut("shots/reorder")]
    public IActionResult ReorderShots([FromBody] ReorderShotsDto dto)
    {
        for (int i = 0; i < dto.ShotIds.Length; i++)
        {
            var shot = _dbContext.Shots.Find(dto.ShotIds[i]);
            if (shot != null)
            {
                shot.Order = i;
                shot.UpdatedTime = DateTime.UtcNow.Ticks;
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    public class EpisodeDto
    {
        public string Name { get; set; } = "";
        public int Duration { get; set; }
        public int Order { get; set; }
    }

    public class ShotDto
    {
        public string FirstFramePrompt { get; set; } = "";
        public string FirstFrameWorkflowType { get; set; } = "Img2Img";
        public string Dialog { get; set; } = "";
        public string VideoPrompt { get; set; } = "";
        public string VideoWorkflowType { get; set; } = "Img2Video";
        public int Order { get; set; }
    }

    public class ReorderShotsDto
    {
        public long[] ShotIds { get; set; } = [];
    }

    public class ReorderEpisodesDto
    {
        public long[] EpisodeIds { get; set; } = [];
    }
}
