// @name:         WorkflowService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  工作流服务实现
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 工作流服务实现
    /// </summary>
    public class WorkflowService : IWorkflowService
    {
        private readonly IProjectDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        public WorkflowService(IProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 获取所有工作流
        /// </summary>
        public async Task<List<Workflow>> GetAllAsync()
            => await _dbContext.Workflows.OrderBy(w => w.Order).ToListAsync();

        /// <summary>
        /// 根据项目ID获取工作流列表
        /// </summary>
        /// <param name="projectId">项目ID</param>
        public async Task<List<Workflow>> GetByProjectAsync(long? projectId)
            => await _dbContext.Workflows
                .Where(w => w.ProjectId == projectId)
                .OrderBy(w => w.Order)
                .ToListAsync();

        /// <summary>
        /// 根据ID获取工作流
        /// </summary>
        /// <param name="id">工作流ID</param>
        public async Task<Workflow> GetByIdAsync(long id)
            => await _dbContext.Workflows.FindAsync(id) ?? new Workflow();

        /// <summary>
        /// 创建工作流
        /// </summary>
        /// <param name="workflow">工作流实体</param>
        public async Task<Workflow> CreateAsync(Workflow workflow)
        {
            workflow.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            workflow.UpdatedTime = workflow.CreatedTime;
            await _dbContext.Workflows.AddAsync(workflow);
            await _dbContext.SaveChangesAsync();
            return workflow;
        }

        /// <summary>
        /// 更新工作流
        /// </summary>
        /// <param name="workflow">工作流实体</param>
        public async Task<Workflow> UpdateAsync(Workflow workflow)
        {
            var existing = await _dbContext.Workflows.FindAsync(workflow.Id);
            if (existing == null) return new Workflow();
            existing.Name = workflow.Name;
            existing.WorkflowType = workflow.WorkflowType;
            existing.ConfigJson = workflow.ConfigJson;
            existing.Order = workflow.Order;
            existing.ProjectId = workflow.ProjectId;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// 删除工作流
        /// </summary>
        /// <param name="id">工作流ID</param>
        public async Task DeleteAsync(long id)
        {
            var workflow = await _dbContext.Workflows.FindAsync(id);
            if (workflow != null)
            {
                _dbContext.Workflows.Remove(workflow);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 创建默认工作流
        /// </summary>
        /// <param name="workflowType">工作流类型</param>
        /// <param name="name">名称</param>
        /// <param name="configJson">配置JSON</param>
        /// <param name="projectId">项目ID</param>
        public async Task<Workflow> CreateDefaultAsync(string workflowType, string name, string configJson, long? projectId = null)
        {
            var maxOrder = await _dbContext.Workflows
                .Where(w => w.ProjectId == projectId && w.WorkflowType == workflowType)
                .MaxAsync(w => (int?)w.Order) ?? -1;
            return await CreateAsync(new Workflow
            {
                Name = name,
                WorkflowType = workflowType,
                ConfigJson = configJson,
                Order = maxOrder + 1,
                ProjectId = projectId
            });
        }
    }
}
