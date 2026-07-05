using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>04.LTX图生视频 — 子图型工作流</summary>
public class Flow_04_ImageToVideo : IComfyFlow<LtxImageToVideoRequest, WorkflowExecuteResponse>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    private string ComfyuiUrl => _configuration["ComfyUI:Url"] ?? "http://127.0.0.1:8188";
    private string WorkflowsDir => _configuration["ComfyUI:WorkflowsDir"] ?? "";

    public Flow_04_ImageToVideo(HttpClient httpClient, IConfiguration configuration, ILogger<Flow_04_ImageToVideo> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowExecuteResponse> ExecuteAsync(LtxImageToVideoRequest req, CancellationToken ct = default)
    {
        var workflow = await ComfyUiHelper.LoadWorkflowAsync("04.LTX图生视频.json", WorkflowsDir);
        var promptApi = ComfyUiHelper.ExpandSubgraphToPromptApi(workflow);

        var parameters = new Dictionary<string, object>();
        ComfyUiHelper.AddIfSet(parameters, "prompt", req.Prompt);
        ComfyUiHelper.AddIfSet(parameters, "width", req.Width);
        ComfyUiHelper.AddIfSet(parameters, "height", req.Height);
        ComfyUiHelper.AddIfSet(parameters, "duration", req.Duration);
        ComfyUiHelper.AddIfSet(parameters, "fps", req.Fps);
        ComfyUiHelper.AddIfSet(parameters, "seed", req.Seed);
        ComfyUiHelper.AddIfSet(parameters, "prompt_enhance", req.PromptEnhance);

        var injectMap = new Dictionary<string, string[]>
        {
            ["prompt"]         = ["266", "value"],
            ["width"]          = ["257", "value"],
            ["height"]         = ["258", "value"],
            ["duration"]       = ["225", "value"],
            ["fps"]            = ["260", "value"],
            ["seed"]           = ["237", "noise_seed"],
            ["prompt_enhance"] = ["330", "value"],
        };
        ComfyUiHelper.InjectParameters(promptApi, injectMap, parameters, _logger);

        var promptId = await ComfyUiHelper.SubmitPromptAsync(_httpClient, ComfyuiUrl, promptApi);
        return await ComfyUiHelper.PollForResultAsync(_httpClient, ComfyuiUrl, promptId,
            maxWait: TimeSpan.FromMinutes(10), logger: _logger);
    }
}
