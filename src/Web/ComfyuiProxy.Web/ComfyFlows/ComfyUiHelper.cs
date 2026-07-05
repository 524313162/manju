using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.ComfyFlows;

/// <summary>
/// ComfyUI 通用工具方法 — 供各个 Flow 按需调用，不强制执行路径
/// </summary>
public static class ComfyUiHelper
{
    private static readonly HashSet<string> SkipNodeTypes = new() { "MarkdownNote", "Reroute" };

    /// <summary>读取工作流文件并解析为 JsonNode</summary>
    public static async Task<JsonNode> LoadWorkflowAsync(string workflowFileName, string workflowsDir)
    {
        var workflowPath = Path.Combine(workflowsDir, workflowFileName);
        if (!File.Exists(workflowPath))
            throw new FileNotFoundException($"工作流文件不存在: {workflowPath}");

        var json = await File.ReadAllTextAsync(workflowPath);
        return JsonNode.Parse(json) ?? throw new InvalidOperationException("解析工作流 JSON 失败");
    }

    /// <summary>扁平 prompt API 构建（无子图）</summary>
    public static JsonObject BuildPromptApi(JsonNode workflow)
    {
        var promptApi = new JsonObject();
        var nodes = workflow["nodes"]?.AsArray() ?? [];
        var links = workflow["links"]?.AsArray() ?? [];

        var execNodes = nodes
            .Where(n => n != null && !SkipNodeTypes.Contains(n["type"]?.GetValue<string>() ?? ""))
            .ToList();
        var execNodeIds = new HashSet<int>(execNodes.Select(n => n!["id"]!.GetValue<int>()));

        foreach (var node in execNodes)
        {
            var nodeId = node!["id"]!.GetValue<int>();
            var nodeType = node["type"]!.GetValue<string>();
            var nodeObj = new JsonObject
            {
                ["class_type"] = nodeType,
                ["inputs"] = new JsonObject()
            };

            var inputs = node["inputs"]?.AsArray() ?? [];
            var widgetsValues = node["widgets_values"]?.AsArray() ?? [];
            int wvIdx = 0;

            for (int i = 0; i < inputs.Count; i++)
            {
                var inp = inputs[i]!;
                var inpName = inp["name"]!.GetValue<string>();
                var linkVal = inp["link"];
                bool hasWidget = inp["widget"] != null;

                if (linkVal != null && linkVal.GetValueKind() != JsonValueKind.Null)
                {
                    int linkId = linkVal.GetValue<int>();
                    foreach (var link in links)
                    {
                        var linkArr = link!.AsArray();
                        if (linkArr[3]!.GetValue<int>() == nodeId && linkArr[4]!.GetValue<int>() == i)
                        {
                            var srcNodeId = linkArr[1]!.GetValue<int>();
                            if (execNodeIds.Contains(srcNodeId))
                            {
                                nodeObj["inputs"]![inpName] = new JsonArray(
                                    srcNodeId.ToString(),
                                    linkArr[2]!.GetValue<int>());
                            }
                            break;
                        }
                    }
                    if (hasWidget) wvIdx++;
                }
                else if (hasWidget && wvIdx < widgetsValues.Count)
                {
                    nodeObj["inputs"]![inpName] = CloneValue(widgetsValues[wvIdx]);
                    wvIdx++;
                }
            }

            promptApi[nodeId.ToString()] = nodeObj;
        }

        return promptApi;
    }

    /// <summary>子图展开为扁平 prompt API</summary>
    public static JsonObject ExpandSubgraphToPromptApi(JsonNode workflow)
    {
        var subgraphs = workflow["definitions"]?["subgraphs"]?.AsArray();
        if (subgraphs == null || subgraphs.Count == 0)
            throw new InvalidOperationException("工作流不包含子图定义");

        var sub = subgraphs[0]!;
        var nodes = sub["nodes"]?.AsArray() ?? [];
        var links = sub["links"]?.AsArray() ?? [];

        var rerouteIds = new HashSet<int>();
        foreach (var n in nodes)
        {
            if (n?["type"]?.GetValue<string>() == "Reroute")
                rerouteIds.Add(n["id"]!.GetValue<int>());
        }

        int TraceReroute(int targetId, int targetSlot)
        {
            int curId = targetId, curSlot = targetSlot;
            while (rerouteIds.Contains(curId))
            {
                bool found = false;
                foreach (var link in links)
                {
                    if (link!["origin_id"]!.GetValue<int>() == curId &&
                        link["origin_slot"]!.GetValue<int>() == 0)
                    {
                        curId = link["target_id"]!.GetValue<int>();
                        curSlot = link["target_slot"]!.GetValue<int>();
                        found = true;
                        break;
                    }
                }
                if (!found) break;
            }
            return curId;
        }

        var linkMap = new Dictionary<(int, int), (string, int)>();
        foreach (var link in links)
        {
            var oid = link!["origin_id"]!.GetValue<int>();
            var os = link["origin_slot"]!.GetValue<int>();
            var tid = link["target_id"]!.GetValue<int>();
            var ts = link["target_slot"]!.GetValue<int>();

            if (rerouteIds.Contains(tid) || rerouteIds.Contains(oid)) continue;
            if (oid == -10) continue;

            int finalTid = TraceReroute(tid, ts);
            if (rerouteIds.Contains(finalTid)) continue;

            linkMap[(finalTid, ts)] = (oid.ToString(), os);
        }

        var promptApi = new JsonObject();
        foreach (var node in nodes)
        {
            if (node == null) continue;
            var ntype = node["type"]?.GetValue<string>() ?? "";
            if (ntype == "MarkdownNote" || ntype == "Reroute") continue;

            var nid = node["id"]!.GetValue<int>();
            var entry = new JsonObject
            {
                ["class_type"] = ntype,
                ["inputs"] = new JsonObject()
            };

            var inpArr = node["inputs"]?.AsArray() ?? [];
            var wv = node["widgets_values"]?.AsArray();
            var wvList = new List<JsonNode?>();
            if (wv != null)
                foreach (var v in wv) wvList.Add(v);

            int wvIdx = 0;
            for (int i = 0; i < inpArr.Count; i++)
            {
                var inp = inpArr[i]!;
                var name = inp["name"]!.GetValue<string>();
                bool hasWidget = inp["widget"] != null;

                if (linkMap.TryGetValue((nid, i), out var src))
                {
                    entry["inputs"]![name] = new JsonArray(src.Item1, src.Item2);
                    if (hasWidget) wvIdx++;
                }
                else if (hasWidget && wvIdx < wvList.Count)
                {
                    entry["inputs"]![name] = CloneValue(wvList[wvIdx]);
                    wvIdx++;
                }
            }

            promptApi[nid.ToString()] = entry;
        }

        return promptApi;
    }

    /// <summary>注入用户参数到 prompt API</summary>
    public static void InjectParameters(JsonObject promptApi, Dictionary<string, string[]> injectMap, Dictionary<string, object> parameters, ILogger? logger = null)
    {
        foreach (var (paramKey, paramValue) in parameters)
        {
            if (paramValue == null) continue;
            if (!injectMap.TryGetValue(paramKey, out var mapping) || mapping == null || mapping.Length < 2)
                continue;

            var nodeId = mapping[0];
            var inputName = mapping[1];

            if (!promptApi.TryGetPropertyValue(nodeId, out var nodeObj) || nodeObj == null)
                continue;

            JsonNode valueNode = paramValue switch
            {
                bool b => JsonValue.Create(b),
                int i => JsonValue.Create(i),
                long l => JsonValue.Create(l),
                float f => JsonValue.Create(f),
                double d => JsonValue.Create(d),
                string s => JsonValue.Create(s),
                _ => JsonValue.Create(paramValue.ToString())
            };

            nodeObj["inputs"]![inputName] = valueNode;
            logger?.LogInformation("注入: {Key}={Val} -> {Node}.{Input}", paramKey, paramValue, nodeId, inputName);
        }
    }

    /// <summary>提交 prompt 到 ComfyUI，返回 prompt_id</summary>
    public static async Task<string> SubmitPromptAsync(HttpClient httpClient, string comfyuiUrl, JsonObject promptApi)
    {
        var payload = new { client_id = "", prompt = promptApi };
        var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        var resp = await httpClient.PostAsync($"{comfyuiUrl}/api/prompt", content);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadAsStringAsync();
        var obj = JsonNode.Parse(result);
        var promptId = obj?["prompt_id"]?.GetValue<string>();

        return promptId ?? throw new InvalidOperationException("ComfyUI 未返回 prompt_id");
    }

    /// <summary>轮询等待结果，提取文本/图片/音频</summary>
    public static async Task<WorkflowExecuteResponse> PollForResultAsync(
        HttpClient httpClient, string comfyuiUrl, string promptId,
        TimeSpan? maxWait = null, TimeSpan? pollInterval = null,
        ILogger? logger = null)
    {
        var response = new WorkflowExecuteResponse { PromptId = promptId };
        var deadline = DateTime.UtcNow + (maxWait ?? TimeSpan.FromMinutes(10));
        var interval = pollInterval ?? TimeSpan.FromSeconds(3);

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(interval);

            try
            {
                var historyUrl = $"{comfyuiUrl}/api/history/{promptId}";
                var historyResp = await httpClient.GetAsync(historyUrl);
                historyResp.EnsureSuccessStatusCode();
                var historyJson = await historyResp.Content.ReadAsStringAsync();
                var history = JsonNode.Parse(historyJson);

                if (history?[promptId] == null) continue;

                var entry = history[promptId]!;
                var outputs = entry["outputs"];

                if (outputs != null)
                {
                    foreach (var kvp in outputs.AsObject())
                    {
                        var nodeOutput = kvp.Value;
                        if (nodeOutput == null) continue;

                        // 文本提取
                        var texts = nodeOutput["text"]?.AsArray();
                        if (texts != null)
                        {
                            foreach (var t in texts)
                            {
                                if (t != null) response.TextOutputs.Add(t.GetValue<string>());
                            }
                        }

                        // 遍历所有键，对任何文件型数组按扩展名归类
                        ExtractAllMedia(nodeOutput, comfyuiUrl, response);
                    }
                }

                response.Success = true;
                logger?.LogInformation("执行完成: {PromptId}, 文本{TextCount}, 图片{ImgCount}, 音频{AudCount}",
                    promptId, response.TextOutputs.Count, response.ImageOutputs.Count, response.AudioOutputs.Count);
                return response;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "轮询异常: {PromptId}", promptId);
            }
        }

        response.Success = false;
        response.Error = $"执行超时（{(maxWait ?? TimeSpan.FromMinutes(10)).TotalSeconds}秒）";
        return response;
    }

    private static readonly HashSet<string> AudioExtensions = new(
        StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".wma", ".m4a", ".opus" };

    /// <summary>遍历节点输出的所有键，自动识别文件型数组并按扩展名归类</summary>
    private static void ExtractAllMedia(JsonNode? nodeOutput, string comfyuiUrl, WorkflowExecuteResponse response)
    {
        if (nodeOutput is not JsonObject obj) return;

        foreach (var (key, value) in obj)
        {
            if (value is not JsonArray arr) continue;
            // 试探性检查第一个元素是否有 filename 字段
            if (arr.Count == 0 || arr[0]?["filename"] == null) continue;

            bool isAudio = key.Equals("audio", StringComparison.OrdinalIgnoreCase);
            foreach (var item in arr)
            {
                if (item == null) continue;
                var filename = item["filename"]?.GetValue<string>() ?? "";
                var subfolder = item["subfolder"]?.GetValue<string>() ?? "";
                var type = item["type"]?.GetValue<string>() ?? "output";

                // 通过扩展名判断类型（键名只是辅助参考）
                var ext = Path.GetExtension(filename);
                var target = AudioExtensions.Contains(ext) || isAudio
                    ? response.AudioOutputs
                    : response.ImageOutputs;

                target.Add(new ImageOutputItem
                {
                    Filename = filename,
                    Subfolder = subfolder,
                    Type = type,
                    Url = $"{comfyuiUrl}/view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={type}"
                });
            }
        }
    }

    public static void AddIfSet(Dictionary<string, object> dict, string key, object? value)
    {
        if (value != null) dict[key] = value;
    }

    public static JsonNode? CloneValue(JsonNode? node)
    {
        return node?.GetValueKind() switch
        {
            JsonValueKind.True => JsonValue.Create(true),
            JsonValueKind.False => JsonValue.Create(false),
            JsonValueKind.Number => JsonValue.Create(node.GetValue<double>()),
            JsonValueKind.String => JsonValue.Create(node.GetValue<string>()),
            _ => JsonValue.Create(node.GetValue<string>())
        };
    }
}
