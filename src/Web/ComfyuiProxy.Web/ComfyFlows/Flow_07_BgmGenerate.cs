using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>07.STABLE-BGM 背景音乐 — 子图型工作流</summary>
public class Flow_07_BgmGenerate : IComfyFlow<StableBgmRequest, WorkflowExecuteResponse>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    private string ComfyuiUrl => _configuration["ComfyUI:Url"] ?? "http://127.0.0.1:8188";
    private string WorkflowsDir => _configuration["ComfyUI:WorkflowsDir"] ?? "D:\\Program Files\\ComfyUI-Installs\\ComfyUI\\ComfyUI\\user\\default\\workflows";

    public Flow_07_BgmGenerate(HttpClient httpClient, IConfiguration configuration, ILogger<Flow_07_BgmGenerate> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowExecuteResponse> ExecuteAsync(StableBgmRequest req, CancellationToken ct = default)
    {
        var workflow = await ComfyUiHelper.LoadWorkflowAsync("09.STABLE-BGM.json", WorkflowsDir);
        var promptApi = ComfyUiHelper.ExpandSubgraphToPromptApi(workflow);

        var parameters = new Dictionary<string, object>();
        ComfyUiHelper.AddIfSet(parameters, "prompt", req.Prompt);
        ComfyUiHelper.AddIfSet(parameters, "duration", req.Duration);
        if (!string.IsNullOrWhiteSpace(req.Category))
            parameters["category"] = req.Category;

        var injectMap = new Dictionary<string, string[]>
        {
            ["prompt"]   = ["98", "user_input"],
            ["duration"] = ["50", "value"],
            ["category"] = ["15", "value"],
        };
        ComfyUiHelper.InjectParameters(promptApi, injectMap, parameters, _logger);

        var promptId = await ComfyUiHelper.SubmitPromptAsync(_httpClient, ComfyuiUrl, promptApi);
        return await ComfyUiHelper.PollForResultAsync(_httpClient, ComfyuiUrl, promptId, logger: _logger);
    }
}
