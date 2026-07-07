using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// 08.ACE-MUSIC 音乐生成 Agent
/// </summary>
public class MusicComposeAgent : ComfyUIAgentBase<AceMusicRequestDto, AceMusicResponse>
{
    public MusicComposeAgent(ComfyuiProxyService proxyService, ILogger<MusicComposeAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ace-music-compose";
    public override string WorkflowFileName => "08.ACE-MUSIC-音乐生成.json";

    protected override async Task<string> BuildWorkflowJsonAsync(AceMusicRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var apiPrompt = ConvertToApiFormat(workflow!);
        var promptObj = apiPrompt["prompt"]?.AsObject();
        if (promptObj == null)
            throw new InvalidOperationException("API prompt 格式异常: 缺少 prompt 字段");

        var textEncodeNode = promptObj["94"]?.AsObject();
        if (textEncodeNode != null)
        {
            var inputs = textEncodeNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["tags"] = dto.Prompt;
                inputs["lyrics"] = dto.Lyrics;
            }
        }

        // 将 tags 也注入到 KSampler (node 3) 的采样参数中
        var kSamplerNode = promptObj["98"]?.AsObject();
        if (kSamplerNode != null)
        {
            var inputs = kSamplerNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["seconds"] = dto.Lyrics.Length / 10 + 15;
            }
        }

        return apiPrompt.ToJsonString();
    }

    protected override void ParseOutputs(JsonObject historyItem, AceMusicResponse result)
    {
        var outputs = historyItem["outputs"]?.AsObject();
        if (outputs == null)
            return;

        foreach (var kvp in outputs)
        {
            var nodeOutput = kvp.Value?.AsObject();
            if (nodeOutput == null) continue;

            var className = nodeOutput["class_type"]?.GetValue<string>();
            if (className != "SaveAudioMP3") continue;

            var images = nodeOutput["images"]?.AsArray();
            if (images == null) continue;

            foreach (var audio in images)
            {
                var audioObj = audio?.AsObject();
                if (audioObj == null) continue;
                var filename = audioObj["filename"]?.GetValue<string>();
                var subfolder = audioObj["subfolder"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(filename))
                {
                    result.AudioUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}");
                }
            }
        }
    }
}
