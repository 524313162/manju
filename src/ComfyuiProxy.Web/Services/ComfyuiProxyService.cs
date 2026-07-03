using System.Text.Json;
using ManjuCraft.Domain.Models;
using ManjuCraft.Domain.Models.ComfyUI;
using ManjuCraft.Infrastructure.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComfyuiProxy.Web.Services;

/// <summary>
/// ComfyUI 代理服务 — 构建 workflow、提交给 ComfyUI、轮询结果、下载输出
/// </summary>
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
        _comfyuiBaseUrl = options.Value.BaseUrl;
        _outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(_outputDir);
    }

    public async Task<bool> CheckComfyuiStatusAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var url = _comfyuiBaseUrl.TrimEnd('/') + "/system_stats";
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> SubmitAsync(string workflowType, string prompt, string? positivePrompt = null, string? imageUrl = null)
    {
        var workflowJson = BuildWorkflow(workflowType, prompt, positivePrompt, imageUrl);
        var result = await _client.SubmitPromptAsync(_comfyuiBaseUrl, workflowJson);
        if (result == null || !result.ContainsKey("prompt_id"))
            throw new Exception("ComfyUI 返回无效响应");
        return result["prompt_id"].ToString()!;
    }

    public async Task<Dictionary<string, ComfyuiHistoryNodeOutputs>> PollAsync(string promptId, int maxAttempts = 120, int pollIntervalMs = 5000)
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

    public async Task<string> DownloadOutputAsync(Dictionary<string, ComfyuiHistoryNodeOutputs> outputs)
    {
        foreach (var nodeOutputs in outputs.Values)
        {
            if (nodeOutputs.Images == null) continue;
            foreach (var fileGroup in nodeOutputs.Images.Values)
            {
                foreach (var file in fileGroup)
                {
                    var filePath = await DownloadAndSaveFileAsync(file);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        _logger.LogInformation("下载输出完成: {Path}", filePath);
                        return filePath;
                    }
                }
            }
        }
        return "";
    }

    private async Task<string> DownloadAndSaveFileAsync(ComfyuiHistoryFile file)
    {
        try
        {
            var localPath = Path.Combine(_outputDir, file.Type, $"{Guid.NewGuid():N}.{GetExtension(file.Type)}");
            var dir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            await _client.DownloadOutputAsync(
                _comfyuiBaseUrl,
                file,
                localPath,
                0);
            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "下载文件失败: {Filename}", file.Filename);
            return "";
        }
    }

    private string GetExtension(string type) => type.ToLowerInvariant() switch
    {
        "webp" or "jpg" or "jpeg" or "png" => "png",
        "mp4" or "webm" => "mp4",
        "mp3" or "wav" => "mp3",
        _ => "png"
    };

    private string BuildWorkflow(string workflowType, string prompt, string? positivePrompt, string? imageUrl)
    {
        return workflowType.ToLowerInvariant() switch
        {
            "txt2img" => BuildTxt2ImgWorkflow(prompt, positivePrompt),
            "img2img" => BuildImg2ImgWorkflow(prompt, positivePrompt, imageUrl),
            "txt2video" => BuildTxt2VideoWorkflow(prompt),
            "img2video" => BuildImg2VideoWorkflow(prompt, imageUrl),
            _ => BuildTxt2ImgWorkflow(prompt, positivePrompt)
        };
    }

    private string BuildTxt2ImgWorkflow(string prompt, string? positivePrompt)
    {
        var positive = !string.IsNullOrEmpty(positivePrompt) ? positivePrompt : prompt;
        var negative = "low quality, blurry, distorted, deformed";

        var workflow = new Dictionary<string, object>
        {
            { "3", new { class_type = "KSampler", inputs = new {
                seed = new Random().Next(0, int.MaxValue), steps = 20, cfg = 7,
                sampler_name = "euler", scheduler = "normal", denoise = 1,
                model = new object[] { "4", 0 }, positive = new object[] { "6", 0 },
                negative = new object[] { "7", 0 }, latent_image = new object[] { "5", 0 }
            }}},
            { "4", new { class_type = "CheckpointLoaderSimple", inputs = new { ckpt_name = "v1-5-pruned-emaonly.ckpt" }}},
            { "5", new { class_type = "EmptyLatentImage", inputs = new { width = 512, height = 512, batch_size = 1 }}},
            { "6", new { class_type = "CLIPTextEncode", inputs = new { text = positive, clip = new object[] { "4", 1 } }}},
            { "7", new { class_type = "CLIPTextEncode", inputs = new { text = negative, clip = new object[] { "4", 1 } }}},
            { "8", new { class_type = "VAEDecode", inputs = new { samples = new object[] { "3", 0 }, vae = new object[] { "4", 2 } }}},
            { "9", new { class_type = "SaveImage", inputs = new { images = new object[] { "8", 0 } }}}
        };

        return JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = false });
    }

    private string BuildImg2ImgWorkflow(string prompt, string? positivePrompt, string? imageUrl)
    {
        var positive = !string.IsNullOrEmpty(positivePrompt) ? positivePrompt : prompt;
        var negative = "low quality, blurry, distorted, deformed";

        var workflow = new Dictionary<string, object>
        {
            { "2", new { class_type = "LoadImage", inputs = new { image = imageUrl ?? "placeholder.png" }}},
            { "3", new { class_type = "KSampler", inputs = new {
                seed = new Random().Next(0, int.MaxValue), steps = 20, cfg = 7,
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

    private string BuildTxt2VideoWorkflow(string prompt)
    {
        return JsonSerializer.Serialize(new { note = "需要配置实际的 txt2video workflow JSON" });
    }

    private string BuildImg2VideoWorkflow(string prompt, string? imageUrl)
    {
        return JsonSerializer.Serialize(new { note = "需要配置实际的 img2video workflow JSON" });
    }
}
