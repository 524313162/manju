using ComfyuiProxy.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/comfyui/upload")]
public class UploadController : ControllerBase
{
    private readonly ComfyuiProxyService _proxyService;

    public UploadController(ComfyuiProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<object>> Upload(
        IFormFile image,
        [FromForm] string? subfolder,
        CancellationToken cancellationToken)
    {
        if (image == null || image.Length == 0)
            return BadRequest("请上传图片文件");

        var uploadDir = Path.Combine(Path.GetTempPath(), "comfyui_uploads");
        Directory.CreateDirectory(uploadDir);
        var filePath = Path.Combine(uploadDir, image.FileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream, cancellationToken);
        }

        var responseBody = await _proxyService.UploadFileAsync(filePath, subfolder, true);

        return Ok(responseBody);
    }
}
