using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

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
        var episode = _dbContext.Episodes
            .Include(e => e.StoryChapter)
            .FirstOrDefault(e => e.Id == id);
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
                Id = e.Id, e.Name, e.Duration, e.Order, e.StoryChapterId, e.ProjectId,
                ShotCount = _dbContext.Shots.Count(s => s.EpisodeId == e.Id),
                ChapterName = e.StoryChapter != null ? e.StoryChapter.ChapterName : null
            })
            .ToList();
        return Ok(new { success = true, data = episodes });
    }

    [HttpPost("project/{projectId}")]
    public IActionResult Create(long projectId, [FromBody] EpisodeDto dto)
    {
        var episode = new Episode
        {
            ProjectId = projectId,
            StoryChapterId = dto.StoryChapterId,
            Name = dto.Name,
            Duration = dto.Duration,
            Order = dto.Order,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
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
        episode.StoryChapterId = dto.StoryChapterId;
        episode.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

    [HttpPut("reorder")]
    public IActionResult Reorder([FromBody] ReorderEpisodesDto dto)
    {
        for (int i = 0; i < dto.EpisodeIds.Length; i++)
        {
            var ep = _dbContext.Episodes.Find(dto.EpisodeIds[i]);
            if (ep != null)
            {
                ep.Order = i;
                ep.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
        public long? StoryChapterId { get; set; }
    }

    public class ReorderEpisodesDto
    {
        public long[] EpisodeIds { get; set; } = [];
    }
}