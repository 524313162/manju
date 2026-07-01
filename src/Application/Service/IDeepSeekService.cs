// @name:         IDeepSeekService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  DeepSeek LLM 服务接口
// @version:      1.0
// @date:         2026-07-01

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// DeepSeek LLM 服务接口
    /// </summary>
    public interface IDeepSeekService
    {
        /// <summary>
        /// 发送聊天消息，获取 AI 回复
        /// </summary>
        /// <param name="messages">消息列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<string> ChatAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据系统提示和用户内容生成回复
        /// </summary>
        /// <param name="systemPrompt">系统提示</param>
        /// <param name="userContent">用户内容</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 聊天消息
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
