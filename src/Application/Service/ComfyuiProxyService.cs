using ManjuCraft.Application.Service.ComfyuiProxy;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
namespace ManjuCraft.Application.Service;

public class ComfyuiProxyService : IComfyuiProxyService
{
    private readonly HttpClient _http;
    private readonly string _proxyUrl;

    public ComfyuiProxyService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _http = httpClientFactory.CreateClient("ai_agent");
        _proxyUrl = configuration.GetValue<string>("ComfyuiProxyUrl") ?? "http://localhost:8288";
    }

    public string GetProxyUrl() => _proxyUrl;

    public async Task<(string? promptId, TResult? result)> SubmitAndPollAsync<TResult>(
        string proxyEndpoint,
        object payload,
        string workflowType,
        int pollIntervalMs = 5000,
        int timeoutMs = 600000,
        CancellationToken cancellationToken = default) where TResult : new()
    {
        try
        {
            var baseUrl = _proxyUrl.TrimEnd('/');
            var endpoint = proxyEndpoint.TrimStart('/');

            // Step 1: Submit workflow to ComfyUI proxy
            var submitRes = await _http.PostAsJsonAsync($"{baseUrl}/{endpoint}", payload, cancellationToken);
            if (!submitRes.IsSuccessStatusCode)
            {
                var errMsg = await submitRes.Content.ReadAsStringAsync(cancellationToken);
                return (null, default(TResult));
            }

            var submitBody = await submitRes.Content.ReadFromJsonAsync<ComfyuiSubmitResponseDto>(cancellationToken);
            var promptId = submitBody?.PromptId;
            if (string.IsNullOrEmpty(promptId))
                return (null, default(TResult));

            // Step 2: Poll for result
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                await Task.Delay(pollIntervalMs, cancellationToken);

                var resultUrl = $"{baseUrl}/api/comfyui/result/{promptId}?workflowType={workflowType}";
                var resultRes = await _http.GetAsync(resultUrl, cancellationToken);
                if (!resultRes.IsSuccessStatusCode)
                    continue;

                var resultBody = await resultRes.Content.ReadFromJsonAsync<ComfyuiResultResponseDto>(cancellationToken);
                if (resultBody?.Success == true && resultBody.Outputs != null)
                {
                    var result = MapResult<TResult>(resultBody.Outputs);
                    return (promptId, result);
                }
            }

            // Step 3: Timeout — return promptId so caller can query manually
            return (promptId, default(TResult));
        }
        catch
        {
            return (null, default(TResult));
        }
    }

    public async Task<(bool success, TResult? result, string? message)> GetResultAsync<TResult>(
        string promptId,
        string workflowType,
        CancellationToken cancellationToken = default) where TResult : new()
    {
        try
        {
            var baseUrl = _proxyUrl.TrimEnd('/');
            var url = $"{baseUrl}/api/comfyui/result/{promptId}?workflowType={workflowType}";

            var res = await _http.GetAsync(url, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                var errMsg = await res.Content.ReadAsStringAsync(cancellationToken);
                return (false, default(TResult), errMsg);
            }

            var resultBody = await res.Content.ReadFromJsonAsync<ComfyuiResultResponseDto>(cancellationToken);
            if (resultBody == null || resultBody.Outputs == null)
            {
                return (false, default(TResult), "未找到结果，任务可能还在执行中");
            }

            if (!resultBody.Success)
            {
                return (false, default(TResult), resultBody.Error ?? "任务执行失败");
            }

            var mapped = MapResult<TResult>(resultBody.Outputs);
            return (true, mapped, null);
        }
        catch (Exception ex)
        {
            return (false, default(TResult), ex.Message);
        }
    }

    public async Task<(bool success, string? message)> InterruptAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _proxyUrl.TrimEnd('/');
            var res = await _http.PostAsync($"{baseUrl}/api/comfyui/interrupt", null, cancellationToken);
            res.EnsureSuccessStatusCode();
            return (true, "已中断当前任务");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static TResult MapResult<TResult>(ComfyuiOutputs output) where TResult : new()
    {
        var result = new TResult();

        switch (result)
        {
            case ComfyuiImageListOutput imgList:
                imgList.Urls = output.ImageUrls?.Select(i => i.Url).ToList() ?? new List<string>();
                break;
            case ComfyuiVideoListOutput vidList:
                vidList.Urls = output.VideoUrls?.Select(v => v.Url).ToList() ?? new List<string>();
                break;
            case ComfyuiAudioListOutput audList:
                audList.Urls = output.AudioUrls?.Select(a => a.Url).ToList() ?? new List<string>();
                break;
            case ComfyuiTextOutput txt:
                txt.Text = output.Text ?? string.Empty;
                break;
            case ComfyuiStoryboardOutput sb:
                sb.ImageUrls = output.ImageUrls?.Select(i => i.Url).ToList() ?? new List<string>();
                break;
            case ComfyuiCharProfileOutput cp:
                cp.ImageUrls = output.ImageUrls?.Select(i => i.Url).ToList() ?? new List<string>();
                break;
        }

        return result;
    }
}
