using System.Text.Json.Serialization;

namespace ManjuCraft.Domain.Models
{
    public class GenerateRequest
    {
        [JsonPropertyName("workflowType")]
        public string WorkflowType { get; set; } = "txt2img";

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = "";

        [JsonPropertyName("positivePrompt")]
        public string? PositivePrompt { get; set; }

        [JsonPropertyName("negativePrompt")]
        public string? NegativePrompt { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; } = 512;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 512;

        [JsonPropertyName("steps")]
        public int Steps { get; set; } = 20;

        [JsonPropertyName("cfg")]
        public float Cfg { get; set; } = 7.0f;

        [JsonPropertyName("seed")]
        public long? Seed { get; set; }
    }

    public class GenerateResponse
    {
        [JsonPropertyName("taskId")]
        public string TaskId { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "processing";

        [JsonPropertyName("progress")]
        public int Progress { get; set; }

        [JsonPropertyName("result")]
        public GenerateResult? Result { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class GenerateResult
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = "image";

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class HealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "ok";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("comfyuiStatus")]
        public string? ComfyuiStatus { get; set; }

        [JsonPropertyName("queueLength")]
        public int QueueLength { get; set; }
    }
}
