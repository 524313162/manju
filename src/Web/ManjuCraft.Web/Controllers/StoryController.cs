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
    private readonly IAiAgentService _aiAgent;

    public StoryController(IStoryService storyService, IProjectService projectService, IProjectDbContext dbContext, IAiAgentService aiAgent)
    {
        _storyService = storyService;
        _projectService = projectService;
        _dbContext = dbContext;
        _aiAgent = aiAgent;
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
        ViewBag.TextProvider = textProvider != null ? new { textProvider.Id, textProvider.Name, textProvider.Model, textProvider.ApiUrl } : null;

        ViewBag.Project = project;
        ViewBag.Story = story;

        return View();
    }

    [HttpGet]
    [Route("[controller]/templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _dbContext.PromptTemplates
            .Where(p => p.TemplateType == "StoryGeneration" || p.TemplateType == "RewriteStory")
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.TemplateType, p.Name, p.Content })
            .ToListAsync();
        
        if (templates.Count == 0)
        {
            var allTemplates = await _dbContext.PromptTemplates
                .Select(p => new { p.Id, p.TemplateType, p.Name, p.Content })
                .ToListAsync();
            
            var data = allTemplates.Count > 0
                ? (object)allTemplates
                : Array.Empty<object>();
            
            var warning = allTemplates.Count == 0
                ? "数据库中没有任何提示词模板，请检查种子数据是否正确加载"
                : $"数据库中仅有 {allTemplates.Count} 个模板，但不包含 StoryGeneration 或 RewriteStory";
            
            return Json(new { success = true, data, warning });
        }
        
        return Json(new { success = true, data = templates });
    }

    [HttpPost]
    public async Task<IActionResult> CreateStory(long projectId, string title)
    {
        var story = new Story { ProjectId = projectId, Title = title ?? "默认剧本" };
        await _storyService.CreateAsync(story);
        return Json(new { success = true, data = story });
    }

    [HttpPost]
    [Route("[controller]/update-summary")]
    public async Task<IActionResult> UpdateSummary([FromForm] long storyId, [FromForm] string title, [FromForm] string summary)
    {
        var story = await _storyService.GetByIdAsync(storyId);
        if (story == null) return Json(new { success = false, message = "剧本不存在" });

        if (!string.IsNullOrWhiteSpace(title))
        {
            story.Title = title;
            await _storyService.UpdateAsync(story);
        }
        if (!string.IsNullOrWhiteSpace(summary) && story.Summary != summary)
        {
            story.Summary = summary;
        }

        await _dbContext.SaveChangesAsync();
        return Json(new { success = true });
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

    [HttpPost]
    [Route("[controller]/bulk-save-ai-result")]
    public async Task<IActionResult> BulkSaveAiResult([FromBody] JsonElement body)
    {
        try
        {
            var root = body;

            var storyId = root.TryGetProperty("storyId", out var sid) ? sid.GetInt64() : 0L;
            var projectId = root.TryGetProperty("projectId", out var pid) ? pid.GetInt64() : 0L;

            // If storyId is 0 but projectId is provided, find or create story
            if (storyId <= 0 && projectId > 0)
            {
                var title = root.TryGetProperty("scriptName", out var nameProp) ? nameProp.GetString()?.Trim() : "";
                if (string.IsNullOrWhiteSpace(title))
                    title = root.TryGetProperty("name", out var nProp) ? nProp.GetString()?.Trim() : "默认剧本";

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
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        story.Title = title;
                        await _storyService.UpdateAsync(story);
                    }
                }
                storyId = story.Id;
            }

            if (storyId <= 0 && projectId <= 0)
                return Json(new { success = false, message = "缺少 storyId 或 projectId" });

            // Save chapters — replace all existing chapters for this story
            if (root.TryGetProperty("chapters", out var chaptersProp) && chaptersProp.ValueKind == JsonValueKind.Array && storyId > 0)
            {
                var existingStory = await _storyService.GetByIdAsync(storyId);
                if (existingStory == null)
                    return Json(new { success = false, message = "剧本不存在" });

                // Remove existing chapters
                var oldChapters = await _dbContext.StoryChapters
                    .Where(c => c.StoryId == storyId)
                    .ToListAsync();
                _dbContext.StoryChapters.RemoveRange(oldChapters);

                var parsedChapters = chaptersProp.EnumerateArray().ToList();
                var storyChapters = parsedChapters.Select((ch, i) => new StoryChapter
                {
                    StoryId = storyId,
                    ChapterNumber = i + 1,
                    ChapterName = ch.TryGetProperty("chapterName", out var cn) ? cn.GetString()
                        : ch.TryGetProperty("ChapterName", out var cn2) ? cn2.GetString()
                        : $"第{i + 1}章",
                    Content = ch.TryGetProperty("content", out var ct) ? ct.GetString()
                        : ch.TryGetProperty("Content", out var ct2) ? ct2.GetString()
                        : "",
                    SortOrder = i + 1
                }).ToList();

                await _dbContext.StoryChapters.AddRangeAsync(storyChapters);
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = "保存成功" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("[controller]/generate")]
    public async Task<IActionResult> GenerateStory(string title, string prompt, string template, long providerId)
    {
        try
        {
            var fullSys = template ?? await GetTemplateContent("StoryGeneration");
            var userMsg = $"标题：{title}\n故事主题：{prompt}";

            var result = await _aiAgent.ChatAsync(providerId, fullSys, userMsg);
            if (!result.success)
                return Json(new { success = false, message = result.message ?? "生成失败" });

            return Json(new { success = true, data = result.data, message = string.Empty });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("[controller]/rewrite")]
    public async Task<IActionResult> RewriteChapter(string content, string template, long providerId, string mode)
    {
        try
        {
            var fullSys = template ?? await GetTemplateContent("RewriteStory");
            var userMsg = $"改写模式：{mode}\n\n原文内容：\n{content}";

            var result = await _aiAgent.ChatAsync(providerId, fullSys, userMsg);
            if (!result.success)
                return Json(new { success = false, message = result.message ?? "改写失败" });

            // Try to parse structured JSON from AI response
            var raw = result.data;
            string rewrittenContent;

            try
            {
                var text = raw;
                var backtickMatch = System.Text.RegularExpressions.Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
                if (backtickMatch.Success) text = backtickMatch.Groups[1].Value.Trim();

                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.TryGetProperty("content", out var ct))
                    rewrittenContent = ct.GetString() ?? raw;
                else
                    rewrittenContent = raw;
            }
            catch
            {
                rewrittenContent = raw;
            }

            return Json(new { success = true, data = rewrittenContent });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<string> GetTemplateContent(string templateType)
    {
        var t = await _dbContext.PromptTemplates
            .Where(p => p.TemplateType == templateType && p.IsDefault)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();
        if (t != null)
            return t.Content;
        t = await _dbContext.PromptTemplates.Where(p => p.TemplateType == templateType).OrderBy(p => p.Id).FirstOrDefaultAsync();
        return t?.Content ?? string.Empty;
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
