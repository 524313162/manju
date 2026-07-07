using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;
using ComfyuiProxy.Web.Services;

namespace ComfyuiProxy.Web.ComfyFlows;

public class AceMusicAgent : ComfyUIAgentBase<AceMusicRequestDto, AceMusicResponse>
{
    public AceMusicAgent(ComfyuiProxyService proxyService, ILogger<AceMusicAgent> logger)
        : base(proxyService, logger) { }

    public override string WorkflowType => "ace-music-compose";
    public override string WorkflowFileName => "08.ACE-MUSIC-音乐生成.json";

    protected override async Task<string> BuildWorkflowJsonAsync(AceMusicRequestDto dto)
    {
        var workflow = await _proxyService.LoadWorkflowAsync(WorkflowFileName);
        if (workflow == null)
            throw new FileNotFoundException($"工作流文件不存在: {WorkflowFileName}");

        var textEncodeNode = workflow["94"]?.AsObject();
        if (textEncodeNode != null)
        {
            var inputs = textEncodeNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["tags"] = dto.Prompt;
                inputs["lyrics"] = dto.Lyrics;
                inputs["bpm"] = dto.Bpm;
                inputs["timesignature"] = dto.Timesignature;
                inputs["language"] = dto.Language;
                inputs["keyscale"] = dto.Keyscale;
                inputs["duration"] = dto.Seconds ?? CalculateSecondsByBpm(dto.Lyrics.Length, dto.Bpm);
            }
        }

        var kSamplerNode = workflow["98"]?.AsObject();
        if (kSamplerNode != null)
        {
            var inputs = kSamplerNode["inputs"]?.AsObject();
            if (inputs != null)
            {
                inputs["seconds"] = dto.Seconds ?? CalculateSecondsByBpm(dto.Lyrics.Length, dto.Bpm);
            }
        }

        return workflow.ToJsonString();
    }

    private static double CalculateSecondsByBpm(int lyricLength, int bpm)
    {
        var charsPerMinute = bpm;
        var minutes = lyricLength / charsPerMinute;
        return minutes * 60;
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

            var audioArray = nodeOutput["audio"]?.AsArray();
            if (audioArray == null) continue;

            foreach (var audio in audioArray)
            {
                var audioObj = audio?.AsObject();
                if (audioObj == null) continue;
                var filename = audioObj["filename"]?.GetValue<string>();
                var subfolder = audioObj["subfolder"]?.GetValue<string>();
                var type = audioObj["type"]?.GetValue<string>() ?? "output";
                if (!string.IsNullOrEmpty(filename))
                {
                    result.AudioUrls.Add($"{_proxyService.GetBaseUrl()}/view?filename={filename}&subfolder={subfolder}&type={type}");
                }
            }
        }
    }
}
