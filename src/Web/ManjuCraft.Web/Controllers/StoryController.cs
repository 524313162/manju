using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.Service.Dtos;
using ManjuCraft.Application.AI;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using System.Text.Json;

namespace ManjuCraft.Web.Controllers;

public class StoryController : Controller
{
    private readonly IStoryService _storyService;
    private readonly IProjectService _projectService;
    private readonly IProjectDbContext _dbContext;
    private readonly IAiClientRegistry _registry;

    public StoryController(IStoryService storyService, IProjectService projectService, IProjectDbContext dbContext, IAiClientRegistry registry)
    {
        _storyService = storyService;
        _projectService = projectService;
        _dbContext = dbContext;
        _registry = registry;
    }

    public async Task<IActionResult> Index(long projectId)
    {
        ViewData["Title"] = "剧本创作";
        ViewBag.HideFooter = true;
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return RedirectToAction("Index", "Projects");

        var stories = await _storyService.GetByProjectIdAsync(projectId);
        Story story;
        if (stories.Count == 0)
        {
            story = new Story { ProjectId = projectId, Title = project.Name };
            await _storyService.CreateAsync(story);
            stories = new List<Story> { story };
        }
        else
        {
            story = stories.First();
        }

        var textProvider = await _dbContext.ApiProviders
            .Where(p => p.Capability == AiCapability.TextToText)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();
        ViewBag.TextProvider = textProvider != null ? new { textProvider.Name, textProvider.Model, textProvider.ApiUrl } : null;

        ViewBag.Project = project;
        ViewBag.Story = story;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateStory(long projectId, string title)
    {
        var story = new Story { ProjectId = projectId, Title = title ?? "默认剧本" };
        await _storyService.CreateAsync(story);
        return Json(new { success = true, data = story });
    }

    [HttpGet]
    public async Task<IActionResult> GetChapters(long storyId)
    {
        var story = await _storyService.GetByIdAsync(storyId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var chapters = await _dbContext.StoryChapters
            .Where(c => c.StoryId == storyId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.ChapterNumber, c.ChapterName, c.Content, c.SortOrder })
            .ToListAsync();

        return Json(new { success = true, data = chapters });
    }

    [HttpPost]
    public async Task<IActionResult> AddChapter([FromBody] ChapterCreateRequestDto req)
    {
        var story = await _storyService.GetByIdAsync(req.StoryId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var maxOrder = await _dbContext.StoryChapters
            .Where(c => c.StoryId == req.StoryId)
            .MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var chapter = new StoryChapter
        {
            StoryId = req.StoryId,
            ChapterNumber = maxOrder + 1,
            ChapterName = req.ChapterName,
            Content = req.Content ?? "",
            SortOrder = maxOrder + 1
        };

        await _dbContext.StoryChapters.AddAsync(chapter);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, data = new { chapter.Id, chapter.ChapterNumber, chapter.ChapterName, chapter.Content, chapter.SortOrder } });
    }

    [HttpPost]
    public async Task<IActionResult> EditChapter([FromBody] ChapterEditRequestDto req)
    {
        var existing = await _dbContext.StoryChapters.FindAsync(req.Id);
        if (existing == null) return Json(new { success = false, message = "章节不存在" });

        existing.ChapterName = req.ChapterName;
        existing.Content = req.Content;

        await _dbContext.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteChapter([FromBody] ChapterDeleteRequestDto req)
    {
        var chapter = await _dbContext.StoryChapters.FindAsync(req.Id);
        if (chapter == null) return Json(new { success = false, message = "章节不存在" });

        _dbContext.StoryChapters.Remove(chapter);

        var remaining = await _dbContext.StoryChapters
            .Where(c => c.StoryId == chapter.StoryId && c.SortOrder > chapter.SortOrder)
            .ToListAsync();

        foreach (var c in remaining)
        {
            c.SortOrder--;
            c.ChapterNumber--;
        }

        await _dbContext.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> BulkAddChapters([FromBody] List<ChapterCreateRequestDto> chapters)
    {
        if (chapters == null || chapters.Count == 0)
            return Json(new { success = false, message = "章节数据为空" });

        var story = await _storyService.GetByIdAsync(chapters[0].StoryId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        var maxOrder = await _dbContext.StoryChapters
            .Where(c => c.StoryId == chapters[0].StoryId)
            .MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var storyChapters = chapters.Select((c, i) => new StoryChapter
        {
            StoryId = chapters[0].StoryId,
            ChapterNumber = maxOrder + i + 1,
            ChapterName = c.ChapterName,
            Content = c.Content,
            SortOrder = maxOrder + i + 1
        }).ToList();

        await _dbContext.StoryChapters.AddRangeAsync(storyChapters);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, data = storyChapters });
    }

    [HttpPost("ImportScript")]
    public async Task<IActionResult> ImportScript(long projectId, [FromBody] ImportScriptRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ScriptJson))
            return Json(new { success = false, message = "请输入 JSON 内容" });

        try
        {
            using var doc = JsonDocument.Parse(req.ScriptJson);
            var root = doc.RootElement;

            var title = root.TryGetProperty("scriptName", out var nameProp) ? nameProp.GetString()?.Trim() : "";
            if (string.IsNullOrWhiteSpace(title))
                title = "默认剧本";

            var stories = await _storyService.GetByProjectIdAsync(projectId);
            Story story;
            if (stories.Count == 0)
            {
                story = new Story { ProjectId = projectId, Title = title };
                await _storyService.CreateAsync(story);
            }
            else
            {
                story = stories.First();
            }

            // 导入 assets
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Object)
            {
                await UpsertAssetGroup(assetsProp, "characters", projectId, "characters");
                await UpsertAssetGroup(assetsProp, "scenes", projectId, "scenes");
                await UpsertAssetGroup(assetsProp, "props", projectId, "props");
                await UpsertAssetGroup(assetsProp, "skills", projectId, "skills");
                await UpsertAssetGroup(assetsProp, "bgm", projectId, "bgm");
            }

            // 导入 chapters
            if (root.TryGetProperty("chapters", out var chaptersProp) && chaptersProp.ValueKind == JsonValueKind.Array)
            {
                var maxOrder = await _dbContext.StoryChapters
                    .Where(c => c.StoryId == story.Id)
                    .MaxAsync(c => (int?)c.SortOrder) ?? 0;

                var parsedChapters = chaptersProp.EnumerateArray().ToList();
                var storyChapters = new List<StoryChapter>();
                for (int i = 0; i < parsedChapters.Count; i++)
                {
                    var ch = parsedChapters[i];
                    var chapterName = ch.TryGetProperty("chapterName", out var cn2) ? cn2.GetString()
                        : ch.TryGetProperty("ChapterName", out var cn3) ? cn3.GetString()
                        : $"第{i + 1}章";
                    var content = ch.TryGetProperty("content", out var ct) ? ct.GetString()
                        : ch.TryGetProperty("Content", out var ct2) ? ct2.GetString()
                        : "";
                    var existingNumber = ch.TryGetProperty("chapterNumber", out var cn4) && cn4.TryGetInt32(out var n4) ? n4 : 0;

                    maxOrder++;
                    storyChapters.Add(new StoryChapter
                    {
                        StoryId = story.Id,
                        ChapterNumber = existingNumber > 0 ? existingNumber : maxOrder,
                        ChapterName = chapterName,
                        Content = content,
                        SortOrder = maxOrder
                    });
                }
                if (storyChapters.Count > 0)
                    await _dbContext.StoryChapters.AddRangeAsync(storyChapters);
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = "导入成功" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "导入失败: " + ex.Message });
        }
    }

    private async Task UpsertAssetGroup(JsonElement assetsProp, string groupName, long projectId, string assetType)
    {
        if (!assetsProp.TryGetProperty(groupName, out var groupProp) || groupProp.ValueKind != JsonValueKind.Array)
            return;

        var existingAssets = await _dbContext.Assets
            .Where(a => a.ProjectId == projectId && a.AssetType == assetType)
            .ToListAsync();

        var existingNames = existingAssets.Select(a => a.Name.ToLower()).ToList();

        foreach (var item in groupProp.EnumerateArray())
        {
            var name = item.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : "";
            var description = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(name))
                continue;

            var existing = existingAssets.FirstOrDefault(a => a.Name.ToLower() == name.ToLower());
            if (existing != null)
            {
                existing.Description = description;
            }
            else
            {
                var asset = new Asset
                {
                    ProjectId = projectId,
                    AssetType = assetType,
                    Name = name,
                    Description = description
                };
                _dbContext.Assets.Add(asset);
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> LoadChaptersForProduction(long projectId)
    {
        var stories = await _storyService.GetByProjectIdAsync(projectId);
        if (stories.Count == 0)
            return Json(new { success = true, data = new List<object>(), storyId = 0L });

        var story = stories[0];
        var chapters = await _dbContext.StoryChapters
            .Where(c => c.StoryId == story.Id)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.StoryId,
                c.ChapterNumber,
                c.ChapterName,
                c.Content,
                c.SortOrder
            })
            .ToListAsync();

        return Json(new
        {
            success = true,
            storyId = story.Id,
            storyTitle = story.Title,
            data = chapters
        });
    }
}
