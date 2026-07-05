using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>01.ZIMAGE文生图 — 子图型工作流</summary>
public class Flow_01_TextToImage : IComfyFlow<ZImageTextToImageRequest, WorkflowExecuteResponse>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    private string ComfyuiUrl => _configuration["ComfyUI:Url"] ?? "http://127.0.0.1:8188";
    private string WorkflowsDir => _configuration["ComfyUI:WorkflowsDir"] ?? "";

    public Flow_01_TextToImage(HttpClient httpClient, IConfiguration configuration, ILogger<Flow_01_TextToImage> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowExecuteResponse> ExecuteAsync(ZImageTextToImageRequest req, CancellationToken ct = default)
    {
        var workflow = await ComfyUiHelper.LoadWorkflowAsync("01.ZIMAGE文生图.json", WorkflowsDir);
        var promptApi = ComfyUiHelper.ExpandSubgraphToPromptApi(workflow);

        var parameters = new Dictionary<string, object>();
        ComfyUiHelper.AddIfSet(parameters, "prompt", req.Prompt);
        ComfyUiHelper.AddIfSet(parameters, "width", req.Width);
        ComfyUiHelper.AddIfSet(parameters, "height", req.Height);
        ComfyUiHelper.AddIfSet(parameters, "seed", req.Seed);
        ComfyUiHelper.AddIfSet(parameters, "steps", req.Steps);

        var injectMap = new Dictionary<string, string[]>
        {
            ["prompt"] = ["27", "text"],
            ["width"]  = ["13", "width"],
            ["height"] = ["13", "height"],
            ["seed"]   = ["3", "seed"],
            ["steps"]  = ["3", "steps"],
        };
        ComfyUiHelper.InjectParameters(promptApi, injectMap, parameters, _logger);

        var promptId = await ComfyUiHelper.SubmitPromptAsync(_httpClient, ComfyuiUrl, promptApi);
        return await ComfyUiHelper.PollForResultAsync(_httpClient, ComfyuiUrl, promptId, logger: _logger);
    }
}
