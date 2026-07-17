using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComfyuiProxy.Web.Controllers;

[ApiController]
[Route("api/comfyui/llm-qwen")]
public class LlmQwenController : ControllerBase
{
    private readonly ComfyUIAgentFactory _agentFactory;

    public LlmQwenController(ComfyUIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// 文本LLM处理大模型，推理
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<object>> Execute(
        [FromBody] LlmQwenRequestDto dto)
    {
        var agent = (LlmQwenAgent)_agentFactory.GetAgent("llm-qwen-execute");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "llm-qwen-execute" });
    }

    /// <summary>
    /// QWen 图生图 (Image Edit 2509)
    /// 使用 07.QWen-图生图2.json 工作流，接收三张图片路径进行图片编辑生成。
    /// 前端先通过 /api/comfyui/upload 上传图片，再将返回的文件名传入此接口。
    /// </summary>
    [HttpPost("image-edit")]
    public async Task<ActionResult<object>> ImageEdit(
        [FromBody] QwenImageEditRequestDto dto)
    {
        var agent = (QwenImageEditAgent)_agentFactory.GetAgent("qwen-image-edit");
        var promptId = await agent.SubmitAsync(dto);
        return Ok(new { promptId, workflowType = "qwen-image-edit" });
    }
}
