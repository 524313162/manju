using System.Net.Http.Json;
using System.Text.Json;
using ManjuCraft.Domain.Models;
using ManjuCraft.Domain.Models.ComfyUI;
using ManjuCraft.Infrastructure.Service;
using Microsoft.Extensions.Options;

namespace ComfyuiProxy.Web.Services;

public class ComfyuiProxyService
{
    private readonly IComfyuiClient _client;
    private readonly ILogger<ComfyuiProxyService> _logger;
    private readonly string _comfyuiBaseUrl;
    private readonly string _outputDir;

    public ComfyuiProxyService(
        IComfyuiClient client,
        ILogger<ComfyuiProxyService> logger,
        IOptionsSnapshot<ComfyuiOptions> options)
    {
        _client = client;
        _logger = logger;
        _comfyuiBaseUrl = options.Value.BaseUrl.TrimEnd('/');
        _outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(_outputDir);
    }

    public async Task<bool> CheckComfyuiStatusAsync()
    {
        try
        {
            var status = await _client.GetConnectionStatusAsync(_comfyuiBaseUrl);
            return status.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(string PromptId, string? Error)> SubmitAsync(GenerateRequest request)
    {
        try
        {
            var workflowJson = BuildWorkflow(request);
            var result = await _client.SubmitPromptAsync(_comfyuiBaseUrl, workflowJson);

            if (result == null || !result.ContainsKey("prompt_id"))
                return (string.Empty, "ComfyUI 返回无效响应");
            
            var promptId = result["prompt_id"]?.ToString() ?? string.Empty;
            return (promptId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交任务失败");
            return (string.Empty, ex.Message);
        }
    }

    public async Task<Dictionary<string, ComfyuiHistoryNodeOutputs>?> PollAsync(string promptId, int maxAttempts = 120, int pollIntervalMs = 5000)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            var history = await _client.GetHistoryAsync(_comfyuiBaseUrl, promptId);
            if (history != null && history.Outputs != null && history.Outputs.Count > 0)
                return history.Outputs;
            await Task.Delay(pollIntervalMs);
        }
        throw new TimeoutException($"任务 {promptId} 超时");
    }

    public async Task<string?> DownloadOutputAsync(Dictionary<string, ComfyuiHistoryNodeOutputs> outputs)
    {
        string? filePath = null;

        foreach (var nodeOutputs in outputs.Values)
        {
            if (nodeOutputs.Images == null) continue;
            foreach (var fileGroup in nodeOutputs.Images.Values)
            {
                foreach (var file in fileGroup)
                {
                    var result = await DownloadAndSaveFileAsync(file);
                    if (!string.IsNullOrEmpty(result))
                    {
                        filePath = result;
                    }
                }
            }
        }

        return filePath;
    }

    private async Task<string?> DownloadAndSaveFileAsync(ComfyuiHistoryFile file)
    {
        try
        {
            var ext = GetExtension(file.Type);
            var localPath = Path.Combine(_outputDir, file.Type, $"{Guid.NewGuid():N}.{ext}");
            var dir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            var url = $"{_comfyuiBaseUrl}/view?filename={Uri.EscapeDataString(file.Filename)}&subdirectory={Uri.EscapeDataString(file.SubPath)}&type={file.Type}";
            var bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(localPath, bytes);

            _logger.LogInformation("下载输出完成: {Path}", localPath);
            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "下载文件失败: {Filename}", file.Filename);
            return null;
        }
    }

    private static string GetExtension(string type) => type.ToLowerInvariant() switch
    {
        "webp" or "jpg" or "jpeg" or "png" => "png",
        "mp4" or "webm" => "mp4",
        "mp3" or "wav" => "mp3",
        _ => "png"
    };

    private string BuildWorkflow(GenerateRequest request)
    {
        return request.WorkflowType.ToLowerInvariant() switch
        {
            "img2img" => BuildImg2ImgWorkflow(request),
            "txt2video" => BuildTxt2VideoWorkflow(request),
            "img2video" => BuildImg2VideoWorkflow(request),
            _ => BuildTxt2ImgWorkflow(request)
        };
    }

    private string BuildTxt2ImgWorkflow(GenerateRequest request)
    {
        var positive = !string.IsNullOrEmpty(request.PositivePrompt) ? request.PositivePrompt : request.Prompt;
        var negative = !string.IsNullOrEmpty(request.NegativePrompt) ? request.NegativePrompt : "low quality, blurry, distorted, deformed";
        var seed = request.Seed ?? (long)Random.Shared.Next(int.MaxValue);
        var width = request.Width > 0 ? request.Width : 512;
        var height = request.Height > 0 ? request.Height : 512;
        var steps = request.Steps > 0 ? request.Steps : 20;
        var cfg = request.Cfg > 0 ? request.Cfg : 7.0f;

        var workflow = new Dictionary<string, object>
        {
            { "3", new { class_type = "KSampler", inputs = new {
                seed = seed, steps = steps, cfg = cfg,
                sampler_name = "euler", scheduler = "normal", denoise = 1,
                model = new object[] { "4", 0 }, positive = new object[] { "6", 0 },
                negative = new object[] { "7", 0 }, latent_image = new object[] { "5", 0 }
            }}},
            { "4", new { class_type = "CheckpointLoaderSimple", inputs = new { ckpt_name = "v1-5-pruned-emaonly.ckpt" }}},
            { "5", new { class_type = "EmptyLatentImage", inputs = new { width = width, height = height, batch_size = 1 }}},
            { "6", new { class_type = "CLIPTextEncode", inputs = new { text = positive, clip = new object[] { "4", 1 } }}},
            { "7", new { class_type = "CLIPTextEncode", inputs = new { text = negative, clip = new object[] { "4", 1 } }}},
            { "8", new { class_type = "VAEDecode", inputs = new { samples = new object[] { "3", 0 }, vae = new object[] { "4", 2 } }}},
            { "9", new { class_type = "SaveImage", inputs = new { images = new object[] { "8", 0 } }}}
        };

        return JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = false });
    }

    private string BuildImg2ImgWorkflow(GenerateRequest request)
    {
        var positive = !string.IsNullOrEmpty(request.PositivePrompt) ? request.PositivePrompt : request.Prompt;
        var negative = !string.IsNullOrEmpty(request.NegativePrompt) ? request.NegativePrompt : "low quality, blurry, distorted, deformed";
        var seed = request.Seed ?? (long)Random.Shared.Next(int.MaxValue);
        var steps = request.Steps > 0 ? request.Steps : 20;
        var cfg = request.Cfg > 0 ? request.Cfg : 7.0f;
        var imageUrl = request.ImageUrl ?? "placeholder.png";

        var workflow = new Dictionary<string, object>
        {
            { "2", new { class_type = "LoadImage", inputs = new { image = imageUrl }}},
            { "3", new { class_type = "KSampler", inputs = new {
                seed = seed, steps = steps, cfg = cfg,
                sampler_name = "euler", scheduler = "normal", denoise = 0.75,
                model = new object[] { "4", 0 }, positive = new object[] { "6", 0 },
                negative = new object[] { "7", 0 }, latent_image = new object[] { "5", 0 }
            }}},
            { "4", new { class_type = "CLIPSetLastLayer", inputs = new { stop_at_clip_layer = 3, clip = new object[] { "42", 0 } }}},
            { "42", new { class_type = "CheckpointLoaderSimple", inputs = new { ckpt_name = "v1-5-pruned-emaonly.ckpt" }}},
            { "5", new { class_type = "VAEEncode", inputs = new { pixels = new object[] { "2", 0 }, vae = new object[] { "42", 2 } }}},
            { "6", new { class_type = "CLIPTextEncode", inputs = new { text = positive, clip = new object[] { "4", 1 } }}},
            { "7", new { class_type = "CLIPTextEncode", inputs = new { text = negative, clip = new object[] { "4", 1 } }}},
            { "8", new { class_type = "VAEDecode", inputs = new { samples = new object[] { "3", 0 }, vae = new object[] { "42", 2 } }}},
            { "9", new { class_type = "SaveImage", inputs = new { images = new object[] { "8", 0 } }}}
        };

        return JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = false });
    }

    private string BuildTxt2VideoWorkflow(GenerateRequest request)
    {
        _logger.LogWarning("txt2video workflow 未实现，使用占位符");
        return JsonSerializer.Serialize(new { note = "txt2video workflow 待配置" });
    }

    private string BuildImg2VideoWorkflow(GenerateRequest request)
    {
        _logger.LogWarning("img2video workflow 未实现，使用占位符");
        return JsonSerializer.Serialize(new { note = "img2video workflow 待配置" });
    }
}
