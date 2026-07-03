using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Infrastructure;
using ManjuCraft.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ManjuCraft.Web.Controllers;

[Route("Assets")]
[Route("Assets/{action=Index}")]
public class AssetsViewComponent : Controller
{
    public IActionResult Index()
    {
        var projectId = Request.Query["projectId"].ToString();
        if (!string.IsNullOrEmpty(projectId))
            ViewData["CurrentProjectId"] = projectId;
        var assetType = Request.Query["assetType"].ToString();
        if (!string.IsNullOrEmpty(assetType))
            ViewData["AssetType"] = assetType;
        return View("~/Views/Assets/Index.cshtml");
    }
}

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

    [HttpGet("{projectId}")]
    public IActionResult List(long projectId, [FromQuery] string? type = null)
    {
        var query = _dbContext.Assets.Where(a => a.ProjectId == projectId);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(a => a.AssetType == type);
        var assets = query.OrderBy(a => a.AssetType).ThenBy(a => a.Order)
            .Select(a => new { a.Id, a.AssetType, a.Name, a.Description, a.ParentId, a.ResourceId, a.Order, a.CreatedTime, a.UpdatedTime })
            .ToList();
        return Ok(new { success = true, data = assets });
    }

    [HttpGet("{projectId}/{assetType}/{id}")]
    public IActionResult GetById(long projectId, string assetType, long id)
    {
        var asset = _dbContext.Assets.FirstOrDefault(a => a.Id == id && a.ProjectId == projectId && a.AssetType == assetType);
        if (asset == null) return NotFound();
        return Ok(new { success = true, data = asset });
    }

    [HttpPost("{projectId}/{assetType}")]
    public IActionResult Create(long projectId, string assetType, [FromBody] AssetCreateDto dto)
    {
        var asset = new Asset
        {
            ProjectId = projectId,
            AssetType = assetType,
            Name = dto.Name,
            Description = dto.Description ?? "",
            ParentId = dto.ParentId,
            Order = dto.Order,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _dbContext.Assets.Add(asset);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { asset.Id, asset.Name, asset.AssetType } });
    }

    [HttpPut("{projectId}/{assetType}/{id}")]
    public IActionResult Update(long projectId, string assetType, long id, [FromBody] AssetUpdateDto dto)
    {
        var asset = _dbContext.Assets.FirstOrDefault(a => a.Id == id && a.ProjectId == projectId && a.AssetType == assetType);
        if (asset == null) return NotFound();
        asset.Name = dto.Name;
        asset.Description = dto.Description ?? "";
        asset.ParentId = dto.ParentId;
        asset.ResourceId = dto.ResourceId;
        asset.Order = dto.Order;
        asset.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("{projectId}/{assetType}/{id}")]
    public IActionResult Delete(long projectId, string assetType, long id)
    {
        var asset = _dbContext.Assets.FirstOrDefault(a => a.Id == id && a.ProjectId == projectId && a.AssetType == assetType);
        if (asset == null) return NotFound();
        _dbContext.Assets.Remove(asset);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpPut("reorder")]
    public IActionResult Reorder([FromBody] ReorderRequest request)
    {
        for (int i = 0; i < request.Ids.Length; i++)
        {
            var asset = _dbContext.Assets.Find(request.Ids[i]);
            if (asset != null)
            {
                asset.Order = i;
                asset.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "排序已更新" });
    }

    [HttpGet("{projectId}/{assetType}/{id}/variants")]
    public IActionResult GetVariants(long projectId, string assetType, long id)
    {
        var variants = _dbContext.Assets
            .Where(a => a.ParentId == id && a.ProjectId == projectId && a.AssetType == assetType)
            .OrderBy(a => a.Order)
            .Select(a => new { a.Id, a.Name, a.Description, a.Order })
            .ToList();
        return Ok(new { success = true, data = variants });
    }

    [HttpPost("{projectId}/{assetType}/{id}/generate-prompt")]
    public async Task<IActionResult> GeneratePrompt(long projectId, string assetType, long id, [FromBody] PromptRequest request)
    {
        var asset = _dbContext.Assets.FirstOrDefault(a => a.Id == id && a.ProjectId == projectId && a.AssetType == assetType);
        if (asset == null) return NotFound();

        var deepSeek = HttpContext.RequestServices.GetRequiredService<ManjuCraft.Application.Service.IDeepSeekService>();

        var systemPrompt = assetType switch
        {
            "Actor" => "你是一个专业的AI绘画提示词生成助手。请根据角色描述，生成适合AI绘画的四视图提示词（正面半身、背面、侧面、三视角），风格为漫剧风格。提示词应包含角色特征、服装、姿势、表情等细节。只输出提示词，不要输出其他内容。",
            "Prop" => "你是一个专业的AI绘画提示词生成助手。请根据道具描述，生成适合AI绘画的双视图提示词（正面、侧面），要求精细建模，风格一致。只输出提示词，不要输出其他内容。",
            "Scene" => "你是一个专业的AI绘画提示词生成助手。请根据场景描述，生成适合AI绘画的背景提示词，风格为漫剧背景风格。提示词应包含场景氛围、光线、色彩、景物细节等。只输出提示词，不要输出其他内容。",
            _ => "你是一个专业的AI绘画提示词生成助手。请根据描述生成适合AI绘画的提示词。只输出提示词，不要输出其他内容。"
        };

        var userContent = $"请为以下{assetType}生成AI绘画提示词：\n名称：{asset.Name}\n描述：{request.Description}";
        var prompt = await deepSeek.GenerateAsync(systemPrompt, userContent);

        asset.Description = prompt;
        asset.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _dbContext.SaveChanges();

        return Ok(new { success = true, data = new { prompt } });
    }

    public class AssetCreateDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public long? ParentId { get; set; }
        public int Order { get; set; }
    }

    public class AssetUpdateDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public long? ParentId { get; set; }
        public long? ResourceId { get; set; }
        public int Order { get; set; }
    }

    public class PromptRequest
    {
        public string Description { get; set; } = "";
    }

    public class ReorderRequest
    {
        public long[] Ids { get; set; } = [];
    }
}