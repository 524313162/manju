using System.Collections.Concurrent;
using ComfyuiProxy.Web.Models;

namespace ComfyuiProxy.Web.Services;

public class TaskManager
{
    readonly ConcurrentDictionary<string, TaskInfo> _tasks = new();
    readonly ConcurrentQueue<string> _queue = new();

    public string Enqueue(string workflowType, string prompt)
    {
        var task = new TaskInfo { WorkflowType = workflowType, Prompt = prompt, Status = "queued" };
        _tasks[task.Id] = task;
        _queue.Enqueue(task.Id);
        return task.Id;
    }

    public TaskInfo? Get(string id) => _tasks.GetValueOrDefault(id);

    public void Update(string id, string status, int progress = 0, string? outputPath = null, string? error = null)
    {
        if (_tasks.TryGetValue(id, out var t))
        {
            t.Status = status;
            t.Progress = progress;
            if (outputPath != null) t.OutputPath = outputPath;
            if (error != null) t.Error = error;
            if (status is "completed" or "failed") t.CompletedAt = DateTime.UtcNow;
        }
    }

    public bool TryDequeue(out string? id) => _queue.TryDequeue(out id);
    public int QueueLength => _queue.Count;
    public IEnumerable<TaskInfo> All() => _tasks.Values.OrderByDescending(t => t.CreatedAt);
}