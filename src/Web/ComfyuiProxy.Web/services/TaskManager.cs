using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using ManjuCraft.Domain.Models;

namespace ComfyuiProxy.Web.Services;

public class TaskInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("workflowType")]
    public string WorkflowType { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("node")]
    public string? Node { get; set; }

    [JsonPropertyName("result")]
    public GenerateResult? Result { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

public class TaskManager
{
    private readonly ConcurrentDictionary<string, TaskInfo> _tasks = new();
    private readonly object _idLock = new();
    private int _idCounter = 0;

    public int QueueLength => _tasks.Values.Count(t => t.Status == "queued" || t.Status == "running");

    public string Enqueue(string workflowType, string prompt)
    {
        var id = $"task-{Interlocked.Increment(ref _idCounter):D6}";
        var info = new TaskInfo
        {
            Id = id,
            WorkflowType = workflowType,
            Prompt = prompt,
            Status = "queued"
        };
        _tasks[id] = info;
        return id;
    }

    public void Update(string id, string status, int progress = 0, string? node = null, string? outputPath = null, string? error = null)
    {
        if (!_tasks.TryGetValue(id, out var info)) return;

        info.Status = status;
        info.Progress = progress;
        info.Node = node;

        if (!string.IsNullOrEmpty(error))
        {
            info.Error = error;
            info.CompletedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(outputPath) && info.Result == null)
        {
            var ext = Path.GetExtension(outputPath).Replace(".", "").ToLowerInvariant();
            var mediaType = ext switch
            {
                "png" or "jpg" or "jpeg" or "webp" => $"image/{ext}",
                "mp4" or "webm" => "video/mp4",
                "mp3" or "wav" => "audio/mpeg",
                _ => "application/octet-stream"
            };

            var size = 0L;
            if (File.Exists(outputPath))
                size = new FileInfo(outputPath).Length;

            var relativeUrl = GenerateOutputUrl(id, ext);
            info.Result = new GenerateResult
            {
                Url = relativeUrl,
                MediaType = mediaType,
                Size = size
            };

            if (status == "completed")
                info.CompletedAt = DateTime.UtcNow;
        }
    }

    public TaskInfo? Get(string id)
    {
        return _tasks.TryGetValue(id, out var info) ? info : null;
    }

    public IEnumerable<TaskInfo> All()
    {
        return _tasks.Values;
    }

    public TaskInfo? TryDequeue()
    {
        foreach (var pair in _tasks)
        {
            if (pair.Value.Status == "queued")
            {
                if (_tasks.TryRemove(pair.Key, out var info))
                    return info;
            }
        }
        return null;
    }

    private static string GenerateOutputUrl(string taskId, string extension)
    {
        return $"/output/{taskId}/image_output";
    }
}
