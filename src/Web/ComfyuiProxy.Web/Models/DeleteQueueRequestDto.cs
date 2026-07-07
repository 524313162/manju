namespace ComfyuiProxy.Web.Models;

/// <summary>
/// 删除队列任务请求
/// </summary>
public class DeleteQueueRequestDto
{
    /// <summary>要删除的 prompt_id 列表</summary>
    public List<string> PromptIds { get; set; } = new();
}
