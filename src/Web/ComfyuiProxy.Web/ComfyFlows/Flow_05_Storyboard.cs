using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>05.HIDREAM分镜 — 扁平型工作流</summary>
public class Flow_05_Storyboard : IComfyFlow<HiDreamStoryboardRequest, WorkflowExecuteResponse>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    private string ComfyuiUrl => _configuration["ComfyUI:Url"] ?? "http://127.0.0.1:8188";
    private string WorkflowsDir => _configuration["ComfyUI:WorkflowsDir"] ?? "";

    public Flow_05_Storyboard(HttpClient httpClient, IConfiguration configuration, ILogger<Flow_05_Storyboard> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowExecuteResponse> ExecuteAsync(HiDreamStoryboardRequest req, CancellationToken ct = default)
    {
        var workflow = await ComfyUiHelper.LoadWorkflowAsync("07.HIDREAM分镜.json", WorkflowsDir);
        var promptApi = ComfyUiHelper.BuildPromptApi(workflow);

        var parameters = new Dictionary<string, object>();
        ComfyUiHelper.AddIfSet(parameters, "prompt", req.Prompt);
        ComfyUiHelper.AddIfSet(parameters, "switch_to_image_edit", req.SwitchToImageEdit);
        ComfyUiHelper.AddIfSet(parameters, "enable_prompt_refine", req.EnablePromptRefine);

        var injectMap = new Dictionary<string, string[]>
        {
            ["prompt"]               = ["171", "value"],
            ["switch_to_image_edit"] = ["154", "value"],
            ["enable_prompt_refine"] = ["177", "value"],
        };
        ComfyUiHelper.InjectParameters(promptApi, injectMap, parameters, _logger);

        var promptId = await ComfyUiHelper.SubmitPromptAsync(_httpClient, ComfyuiUrl, promptApi);
        return await ComfyUiHelper.PollForResultAsync(_httpClient, ComfyuiUrl, promptId, logger: _logger);
    }
}
