using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.AI;
using ManjuCraft.Application.Service;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;
using System.Text.Json;
using IoFile = System.IO.File;

namespace ManjuCraft.Web.Controllers.Api;

[Route("api/v1/ai")]
[ApiController]
public class AIGenerationController : ControllerBase
{
    private readonly ProjectDbContext _db;
    private readonly IAiAgentService _aiService;
    private readonly IWebHostEnvironment _env;

    public AIGenerationController(ProjectDbContext db, IAiAgentService aiService, IWebHostEnvironment env)
    {
        _db = db;
        _aiService = aiService;
        _env = env;
    }

    [HttpPost("extract-asset-info")]
    public async Task<IActionResult> ExtractAssetInfo([FromBody] JsonElement body)
    {
        try
        {
            var providerId = body.TryGetProperty("providerId", out var pid) ? pid.GetInt64() : 0L;
            var template = body.TryGetProperty("template", out var tpl) ? tpl.GetString() : null;
            var chapterIds = body.TryGetProperty("chapterIds", out var cids) && cids.ValueKind == JsonValueKind.Array
                ? cids.EnumerateArray().Select(e => e.GetInt64()).ToList()
                : new List<long>();

            if (chapterIds.Count == 0)
                return Ok(new { success = false, message = "请选择至少一个章节" });

            if (providerId <= 0)
                return Ok(new { success = false, message = "请选择 AI 提供者" });

            var chapters = await _db.StoryChapters
                .Where(c => chapterIds.Contains(c.Id))
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            if (chapters.Count == 0)
                return Ok(new { success = false, message = "未找到选中的章节" });

            var storyId = chapters[0].StoryId;
            var story = await _db.Stories.FindAsync(storyId);
            if (story == null)
                return Ok(new { success = false, message = "剧本不存在" });
            var projectId = story.ProjectId;

            var existingAssets = await _db.Assets
                .Where(a => a.ProjectId == projectId)
                .Select(a => new { a.Name, a.AssetType, a.Description })
                .ToListAsync();

            var existingAssetsText = string.Join("\n", existingAssets.Select(a => $"- {a.Name} ({a.AssetType})"));

            var chaptersText = string.Join("\n\n", chapters.Select(c =>
                $"【第{c.ChapterNumber}章 {c.ChapterName}】\n{c.Content}"));

            var templateContent = template;
            if (string.IsNullOrEmpty(templateContent))
            {
                var t = await _db.PromptTemplates
                    .Where(p => p.TemplateType == "AssetExtraction")
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync();
                templateContent = t?.Content ?? "";
            }

            var userMsg = $"## 已有项目资产\n{existingAssetsText}\n\n## 章节内容\n{chaptersText}\n\n请严格按JSON格式输出提取的资产信息。";

            var result = await _aiService.ChatAsync(providerId, templateContent, userMsg);
            if (!result.success)
                return Ok(new { success = false, message = result.message ?? "提取失败" });

            if (result.isComfyui)
                return Ok(new { success = true, data = result.data, promptId = result.promptId, workflowType = result.workflowType, isComfyui = true });

            return Ok(new { success = true, data = result.data });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("generate-frame-image")]
    public async Task<IActionResult> GenerateFrameImage([FromBody] GenerateFrameImageRequestDto req)
    {
        try
        {
            var frame = await _db.ShotFrames
                .Where(f => f.ShotId == req.ShotId)
                .OrderBy(f => f.Order)
                .Skip(req.FrameIdx)
                .FirstOrDefaultAsync();

            if (frame == null)
                return Ok(new { success = false, message = "未找到帧" });

            var prompt = frame.Description;
            if (string.IsNullOrWhiteSpace(prompt))
                return Ok(new { success = false, message = "帧描述为空，无法生成" });

            var result = await _aiService.GenerateFrameImageAsync(prompt, req.Width, req.Height, req.ProviderId > 0 ? req.ProviderId : null);

            if (!result.success)
                return Ok(new { success = false, message = result.message ?? "生成失败" });

            if (result.isComfyui)
                return Ok(new { success = true, promptId = result.promptId, workflowType = result.workflowType, isComfyui = true });

            return Ok(new { success = true, data = result.data });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("generate-frame-image-with-assets")]
    public async Task<IActionResult> GenerateFrameImageWithAssets([FromBody] GenerateFrameImageWithAssetsRequestDto req)
    {
        try
        {
            var frame = await _db.ShotFrames
                .Where(f => f.ShotId == req.ShotId)
                .OrderBy(f => f.Order)
                .Skip(req.FrameIdx)
                .FirstOrDefaultAsync();

            if (frame == null)
                return Ok(new { success = false, message = "未找到帧" });

            var prompt = !string.IsNullOrWhiteSpace(req.Prompt) ? req.Prompt : frame.Description;
            if (string.IsNullOrWhiteSpace(prompt))
                return Ok(new { success = false, message = "帧描述为空，无法生成" });

            var shotFrameAssets = await _db.ShotFrameAssets
                .Where(sfa => sfa.ShotFrameId == frame.Id)
                .Include(sfa => sfa.Asset)
                .ThenInclude(a => a.Resource)
                .ToListAsync();

            var assetsWithImages = shotFrameAssets
                .Where(sfa => sfa.Asset?.Resource != null && !string.IsNullOrEmpty(sfa.Asset.Resource.FilePath))
                .Select(sfa => new { Name = sfa.Asset.Name, FilePath = sfa.Asset.Resource.FilePath })
                .ToList();

            var wwwRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string compositeImagePath = null;
            if (assetsWithImages.Count > 0)
            {
                var tempsDir = Path.Combine(wwwRoot, "temps");
                var fileName = $"composite_{req.ShotId}_{req.FrameIdx}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
                var fullPaths = new List<(string imagePath, string assetName)>();
                foreach (var asset in assetsWithImages)
                {
                    var assetPath = asset.FilePath;
                    if (!Path.IsPathRooted(assetPath))
                        assetPath = Path.Combine(wwwRoot, assetPath.TrimStart('/'));
                    if (!IoFile.Exists(assetPath))
                        return Ok(new { success = false, message = $"资产图片不存在: {asset.Name}" });
                    fullPaths.Add((assetPath, asset.Name));
                }

                compositeImagePath = ImageCompositeHelper.CreateCompositeImage(fullPaths, tempsDir, fileName).compositeFilePath;
            }

            var result = await _aiService.SubmitFrameImageWithAssetsAsync(prompt, compositeImagePath, req.ProviderId > 0 ? req.ProviderId : null);

            if (!result.success)
                return Ok(new { success = false, message = result.message ?? "提交任务失败" });

            return Ok(new { success = true, promptId = result.promptId, workflowType = result.workflowType, isComfyui = true });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("extract-shot-frame-assets")]
    public async Task<IActionResult> ExtractShotFrameAssets([FromBody] JsonElement body)
    {
        try
        {
            var providerId = body.TryGetProperty("providerId", out var pid) ? pid.GetInt64() : 0L;
            var shotDescription = body.TryGetProperty("shotDescription", out var sd) ? sd.GetString() : "";
            var frameDescriptions = body.TryGetProperty("frameDescriptions", out var fds) && fds.ValueKind == JsonValueKind.Array
                ? fds.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                : new List<string>();
            var projectId = body.TryGetProperty("projectId", out var pj) ? pj.GetInt64() : 0L;
            var shotId = body.TryGetProperty("shotId", out var sid) ? sid.GetInt64() : 0L;
            var selectedAssetNames = body.TryGetProperty("selectedAssetNames", out var san) && san.ValueKind == JsonValueKind.Array
                ? san.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();

            if (providerId <= 0)
                return Ok(new { success = false, message = "请选择 AI 提供者" });

            if (string.IsNullOrWhiteSpace(shotDescription) && frameDescriptions.Count == 0)
                return Ok(new { success = false, message = "分镜描述和帧描述不能都为空" });

            var existingAssets = await _db.Assets
                .Where(a => a.ProjectId == projectId)
                .Select(a => new { a.Name, a.AssetType, a.Description })
                .ToListAsync();

            var existingAssetsText = string.Join("\n", existingAssets.Select(a => $"- {a.Name} ({a.AssetType})"));
            var selectedAssetsText = selectedAssetNames.Count > 0
                ? "\n## 用户选中的已有资产（用于参考，不重复输出）\n" + string.Join("\n", selectedAssetNames.Select(n => $"- {n}"))
                : "";
            var frameText = string.Join("\n\n", frameDescriptions.Select((d, i) => $"【帧{i}】\n{d}"));

            var userMsg = $"## 已有项目资产\n{existingAssetsText}{selectedAssetsText}\n\n## 分镜描述\n{shotDescription}\n\n## 帧描述\n{frameText}\n\n请严格按JSON格式输出提取的新资产信息。";

            var t = await _db.PromptTemplates
                .Where(p => p.TemplateType == "ShotFrameAssetExtraction")
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();
            var templateContent = t?.Content ?? "";

            var result = await _aiService.ChatAsync(providerId, templateContent, userMsg);
            if (!result.success)
                return Ok(new { success = false, message = result.message ?? "提取失败" });

            return Ok(new { success = true, data = result.data });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

}

public class GenerateFrameImageRequestDto
{
    public long ShotId { get; set; }
    public int FrameIdx { get; set; }
    public long ProviderId { get; set; }
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 576;
}

public class GenerateFrameImageWithAssetsRequestDto
{
    public long ShotId { get; set; }
    public int FrameIdx { get; set; }
    public long ProviderId { get; set; }
    public string? Prompt { get; set; }
}
