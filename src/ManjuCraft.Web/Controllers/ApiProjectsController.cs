using Microsoft.AspNetCore.Mvc;
using ManjuCraft.Application.Service;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApiProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IComfyuiConnectionService _comfyuiConnectionService;
    private readonly ILogger<ApiProjectsController> _logger;

    public ApiProjectsController(IProjectService projectService, IComfyuiConnectionService comfyuiConnectionService, ILogger<ApiProjectsController> logger)
    {
        _projectService = projectService;
        _comfyuiConnectionService = comfyuiConnectionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var projects = await _projectService.GetAllAsync();
        return Ok(new { success = true, data = projects });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var project = await _projectService.GetByIdAsync(id);
        return Ok(new { success = true, data = project });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Ok(new { success = false, message = "请输入项目名称" });

        var project = await _projectService.CreateAsync(new Domain.Models.Project
        {
            Name = dto.Name,
            ComfyuiConfigJson = dto.ComfyuiConfigJson ?? ""
        });
        return Ok(new { success = true, data = new { project.Id, project.Name } });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _projectService.DeleteAsync(id);
        return Ok(new { success = true, message = "删除成功" });
    }

    [HttpGet("{id}/comfyui-status")]
    public async Task<IActionResult> GetComfyuiStatus(long id)
    {
        var project = await _projectService.GetByIdAsync(id);
        var apiUrl = "http://localhost:8188";
        if (!string.IsNullOrEmpty(project?.ComfyuiConfigJson))
        {
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(project.ComfyuiConfigJson);
                apiUrl = config?.GetValueOrDefault("apiUrl") ?? apiUrl;
            }
            catch { }
        }
        var status = await _comfyuiConnectionService.TestConnectionAsync(apiUrl);
        return Ok(new { success = true, data = status });
    }

    public class ProjectCreateDto
    {
        public string Name { get; set; } = "";
        public string? ComfyuiConfigJson { get; set; }
    }
}
