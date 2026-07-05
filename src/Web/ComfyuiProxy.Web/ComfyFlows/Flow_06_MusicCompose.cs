using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>06.ACE-MUSIC 音乐生成 — 扁平型工作流</summary>
public class Flow_06_MusicCompose : IComfyFlow<AceMusicRequest, WorkflowExecuteResponse>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    private string ComfyuiUrl => _configuration["ComfyUI:Url"] ?? "http://127.0.0.1:8188";
    private string WorkflowsDir => _configuration["ComfyUI:WorkflowsDir"] ?? "";

    public Flow_06_MusicCompose(HttpClient httpClient, IConfiguration configuration, ILogger<Flow_06_MusicCompose> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowExecuteResponse> ExecuteAsync(AceMusicRequest req, CancellationToken ct = default)
    {
        var workflow = await ComfyUiHelper.LoadWorkflowAsync("08.ACE-MUSIC.json", WorkflowsDir);
        var promptApi = ComfyUiHelper.BuildPromptApi(workflow);

        var parameters = new Dictionary<string, object>();
        ComfyUiHelper.AddIfSet(parameters, "tags", req.Tags);
        ComfyUiHelper.AddIfSet(parameters, "lyrics", req.Lyrics);
        ComfyUiHelper.AddIfSet(parameters, "duration", req.Duration);
        ComfyUiHelper.AddIfSet(parameters, "seed", req.Seed);
        ComfyUiHelper.AddIfSet(parameters, "bpm", req.Bpm);

        var injectMap = new Dictionary<string, string[]>
        {
            ["tags"]     = ["94", "tags"],
            ["lyrics"]   = ["94", "lyrics"],
            ["duration"] = ["99", "value"],
            ["seed"]     = ["109", "noise_seed"],
            ["bpm"]      = ["94", "bpm"],
        };
        ComfyUiHelper.InjectParameters(promptApi, injectMap, parameters, _logger);

        var promptId = await ComfyUiHelper.SubmitPromptAsync(_httpClient, ComfyuiUrl, promptApi);
        return await ComfyUiHelper.PollForResultAsync(_httpClient, ComfyuiUrl, promptId, logger: _logger);
    }
}
