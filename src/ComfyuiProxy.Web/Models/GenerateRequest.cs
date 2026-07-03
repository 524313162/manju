namespace ComfyuiProxy.Web.Models;

public class GenerateRequest
{
    public string WorkflowType { get; set; } = "txt2img";
    public string Prompt { get; set; } = "";
    public string? PositivePrompt { get; set; }
    public string? ImageUrl { get; set; }
}

public class GenerateResponse
{
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "processing";
    public int Progress { get; set; }
    public GenerateResult? Result { get; set; }
    public string? Error { get; set; }
}

public class GenerateResult
{
    public string Url { get; set; } = "";
    public string MediaType { get; set; } = "image";
    public long Size { get; set; }
}

public class TaskInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkflowType { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Status { get; set; } = "queued";
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? OutputPath { get; set; }
    public string? Error { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; } = "ok";
    public string Version { get; set; } = "1.0.0";
    public string? ComfyuiStatus { get; set; }
    public int QueueLength { get; set; }
}