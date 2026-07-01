// @name:         IWorkflowService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  工作流服务接口
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 工作流服务接口
    /// </summary>
    public interface IWorkflowService
    {
        /// <summary>
        /// 获取所有工作流
        /// </summary>
        Task<List<Workflow>> GetAllAsync();

        /// <summary>
        /// 根据项目ID获取工作流列表
        /// </summary>
        /// <param name="projectId">项目ID</param>
        Task<List<Workflow>> GetByProjectAsync(long? projectId);

        /// <summary>
        /// 根据ID获取工作流
        /// </summary>
        /// <param name="id">工作流ID</param>
        Task<Workflow> GetByIdAsync(long id);

        /// <summary>
        /// 创建工作流
        /// </summary>
        /// <param name="workflow">工作流实体</param>
        Task<Workflow> CreateAsync(Workflow workflow);

        /// <summary>
        /// 更新工作流
        /// </summary>
        /// <param name="workflow">工作流实体</param>
        Task<Workflow> UpdateAsync(Workflow workflow);

        /// <summary>
        /// 删除工作流
        /// </summary>
        /// <param name="id">工作流ID</param>
        Task DeleteAsync(long id);

        /// <summary>
        /// 创建默认工作流
        /// </summary>
        /// <param name="workflowType">工作流类型</param>
        /// <param name="name">名称</param>
        /// <param name="configJson">配置JSON</param>
        /// <param name="projectId">项目ID</param>
        Task<Workflow> CreateDefaultAsync(string workflowType, string name, string configJson, long? projectId = null);
    }
}
