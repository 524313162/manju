using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service;
using ManjuCraft.Infrastructure.Service;
using ManjuCraft.Web.Services;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Web.Controllers;

[Controller]
public class SettingsView : Controller
{
    [HttpGet("Settings")]
    [HttpGet("Settings/Index")]
    public IActionResult Index()
    {
        return View("~/Views/Settings/Index.cshtml");
    }

    }

[ApiController]
[Route("api/v1/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IComfyuiConnectionService _comfyuiConnectionService;
    private readonly IFfmpegService _ffmpeg;
    private readonly ProjectDbContext _dbContext;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IComfyuiConnectionService comfyuiConnectionService,
        IFfmpegService ffmpeg,
        ProjectDbContext dbContext,
        ILogger<SettingsController> logger)
    {
        _comfyuiConnectionService = comfyuiConnectionService;
        _ffmpeg = ffmpeg;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("comfyui")]
    public IActionResult GetComfyuiSettings()
    {
        return Ok(new { success = true, data = new { apiUrl = "http://localhost:8188", wsUrl = "ws://localhost:8188/ws", outputDir = "" } });
    }

    [HttpPut("comfyui")]
    public IActionResult UpdateComfyuiSettings([FromBody] ComfyuiSettingsDto dto)
    {
        return Ok(new { success = true, message = "设置已保存" });
    }

    [HttpGet("comfyui/test")]
    public async Task<IActionResult> TestComfyuiConnection([FromQuery] string apiUrl = "http://localhost:8188")
    {
        try
        {
            var status = await _comfyuiConnectionService.TestConnectionAsync(apiUrl);
            return Ok(new { success = true, data = status, message = "连接成功" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("ffmpeg")]
    public IActionResult FfmpegStatus()
    {
        var (available, version) = _ffmpeg.Check();
        return Ok(new { success = true, data = new { available, version = version ?? "unknown" } });
    }

    // ===== ApiProvider CRUD =====

    [HttpGet("apiproviders")]
    public IActionResult ListApiProviders()
    {
        var providers = _dbContext.ApiProviders
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.ApiUrl, p.IsDefault, p.IsActive, p.CreatedTime, p.UpdatedTime })
            .ToList();
        return Ok(new { success = true, data = providers });
    }

    [HttpGet("apiproviders/{id}")]
    public IActionResult GetApiProvider(long id)
    {
        var provider = _dbContext.ApiProviders.Find(id);
        if (provider == null) return NotFound();
        return Ok(new { success = true, data = provider });
    }

    [HttpPost("apiproviders")]
    public IActionResult CreateApiProvider([FromBody] ApiProviderDto dto)
    {
        var provider = new ApiProvider
        {
            Name = dto.Name,
            ApiUrl = dto.ApiUrl,
            ApiKey = dto.ApiKey ?? "",
            ConfigJson = dto.ConfigJson ?? "{}",
            IsDefault = dto.IsDefault,
            IsActive = dto.IsActive,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        if (provider.IsDefault)
            ClearDefaultApiProvider();

        _dbContext.ApiProviders.Add(provider);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { provider.Id, provider.Name } });
    }

    [HttpPut("apiproviders/{id}")]
    public IActionResult UpdateApiProvider(long id, [FromBody] ApiProviderDto dto)
    {
        var provider = _dbContext.ApiProviders.Find(id);
        if (provider == null) return NotFound();
        provider.Name = dto.Name;
        provider.ApiUrl = dto.ApiUrl;
        provider.ApiKey = dto.ApiKey ?? "";
        provider.ConfigJson = dto.ConfigJson ?? "{}";
        provider.IsDefault = dto.IsDefault;
        provider.IsActive = dto.IsActive;
        provider.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (provider.IsDefault)
            ClearDefaultApiProvider(id);

        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("apiproviders/{id}")]
    public IActionResult DeleteApiProvider(long id)
    {
        var provider = _dbContext.ApiProviders.Find(id);
        if (provider == null) return NotFound();
        _dbContext.ApiProviders.Remove(provider);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    private void ClearDefaultApiProvider(long? excludeId = null)
    {
        var query = _dbContext.ApiProviders.Where(p => p.IsDefault);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        foreach (var p in query)
            p.IsDefault = false;
    }

    // ===== PromptTemplate CRUD =====

    [HttpGet("prompttemplates")]
    public IActionResult ListPromptTemplates()
    {
        var templates = _dbContext.PromptTemplates
            .OrderBy(t => t.TemplateType)
            .ThenBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.TemplateType, t.Content, t.IsDefault, t.CreatedTime, t.UpdatedTime })
            .ToList();
        return Ok(new { success = true, data = templates });
    }

    [HttpGet("prompttemplates/{id}")]
    public IActionResult GetPromptTemplate(long id)
    {
        var template = _dbContext.PromptTemplates.Find(id);
        if (template == null) return NotFound();
        return Ok(new { success = true, data = template });
    }

    [HttpPost("prompttemplates")]
    public IActionResult CreatePromptTemplate([FromBody] PromptTemplateDto dto)
    {
        var template = new PromptTemplate
        {
            Name = dto.Name,
            TemplateType = dto.TemplateType,
            Content = dto.Content,
            IsDefault = dto.IsDefault,
            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _dbContext.PromptTemplates.Add(template);
        _dbContext.SaveChanges();
        return Ok(new { success = true, data = new { template.Id, template.Name } });
    }

    [HttpPut("prompttemplates/{id}")]
    public IActionResult UpdatePromptTemplate(long id, [FromBody] PromptTemplateDto dto)
    {
        var template = _dbContext.PromptTemplates.Find(id);
        if (template == null) return NotFound();
        template.Name = dto.Name;
        template.TemplateType = dto.TemplateType;
        template.Content = dto.Content;
        template.IsDefault = dto.IsDefault;
        template.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("prompttemplates/{id}")]
    public IActionResult DeletePromptTemplate(long id)
    {
        var template = _dbContext.PromptTemplates.Find(id);
        if (template == null) return NotFound();
        _dbContext.PromptTemplates.Remove(template);
        _dbContext.SaveChanges();
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpGet("backup")]
    public IActionResult BackupDb([FromQuery] string path = "")
    {
        return Ok(new { success = true, message = "备份功能待实现" });
    }

    public class ComfyuiSettingsDto
    {
        public string ApiUrl { get; set; } = "";
        public string WsUrl { get; set; } = "";
        public string OutputDir { get; set; } = "";
    }

    public class ApiProviderDto
    {
        public string Name { get; set; } = "";
        public string ApiUrl { get; set; } = "";
        public string? ApiKey { get; set; }
        public string? ConfigJson { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }

    public class PromptTemplateDto
    {
        public string Name { get; set; } = "";
        public string TemplateType { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsDefault { get; set; }
    }
}