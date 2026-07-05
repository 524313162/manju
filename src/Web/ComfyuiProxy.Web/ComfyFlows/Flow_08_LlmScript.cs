using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>08.LLM-QWen 大模型脚本生成 — 扁平型工作流</summary>
public class Flow_08_LlmScript : IComfyFlow<LlmQwenRequest, WorkflowExecuteResponse>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    private string ComfyuiUrl => _configuration["ComfyUI:Url"] ?? "http://127.0.0.1:8188";
    private string WorkflowsDir => _configuration["ComfyUI:WorkflowsDir"] ?? "";

    public Flow_08_LlmScript(HttpClient httpClient, IConfiguration configuration, ILogger<Flow_08_LlmScript> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowExecuteResponse> ExecuteAsync(LlmQwenRequest req, CancellationToken ct = default)
    {
        var workflow = await ComfyUiHelper.LoadWorkflowAsync("20.LLM-QWen.json", WorkflowsDir);
        var promptApi = ComfyUiHelper.BuildPromptApi(workflow);

        var parameters = new Dictionary<string, object>();
        ComfyUiHelper.AddIfSet(parameters, "prompt", req.Prompt);
        ComfyUiHelper.AddIfSet(parameters, "max_length", req.MaxLength);

        var injectMap = new Dictionary<string, string[]>
        {
            ["prompt"]     = ["7", "prompt"],
            ["max_length"] = ["7", "max_length"],
        };
        ComfyUiHelper.InjectParameters(promptApi, injectMap, parameters, _logger);

        var promptId = await ComfyUiHelper.SubmitPromptAsync(_httpClient, ComfyuiUrl, promptApi);
        return await ComfyUiHelper.PollForResultAsync(_httpClient, ComfyuiUrl, promptId, logger: _logger);
    }
}
