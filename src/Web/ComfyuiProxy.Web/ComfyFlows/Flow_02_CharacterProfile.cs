using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>02.ZIMAGE人物档案 — 扁平型工作流</summary>
public class Flow_02_CharacterProfile : IComfyFlow<ZImageCharacterProfileRequest, WorkflowExecuteResponse>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    private string ComfyuiUrl => _configuration["ComfyUI:Url"] ?? "http://127.0.0.1:8188";
    private string WorkflowsDir => _configuration["ComfyUI:WorkflowsDir"] ?? "";

    public Flow_02_CharacterProfile(HttpClient httpClient, IConfiguration configuration, ILogger<Flow_02_CharacterProfile> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowExecuteResponse> ExecuteAsync(ZImageCharacterProfileRequest req, CancellationToken ct = default)
    {
        var workflow = await ComfyUiHelper.LoadWorkflowAsync("02.ZIMAGE人物档案.json", WorkflowsDir);
        var promptApi = ComfyUiHelper.BuildPromptApi(workflow);

        var parameters = new Dictionary<string, object>
        {
            ["prompt"]          = req.Prompt,
            ["layout_prompt"]   = req.LayoutPrompt,
            ["negative_prompt"] = req.NegativePrompt,
        };

        var injectMap = new Dictionary<string, string[]>
        {
            ["prompt"]          = ["202", "text"],
            ["layout_prompt"]   = ["200", "text"],
            ["negative_prompt"] = ["201", "text"],
        };
        ComfyUiHelper.InjectParameters(promptApi, injectMap, parameters, _logger);

        var promptId = await ComfyUiHelper.SubmitPromptAsync(_httpClient, ComfyuiUrl, promptApi);
        return await ComfyUiHelper.PollForResultAsync(_httpClient, ComfyuiUrl, promptId, logger: _logger);
    }
}
