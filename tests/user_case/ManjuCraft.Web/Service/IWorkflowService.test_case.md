# IWorkflowService / WorkflowService 测试用例文档

## 基本信息
- **服务名称**: IWorkflowService / WorkflowService
- **命名空间**: ManjuCraft.Web.Service
- **文件位置**: Service/IWorkflowService.cs, Service/WorkflowService.cs
- **作者**: AI Assistant
- **版本**: 1.0
- **日期**: 2026-06-30

## 测试覆盖情况

| 测试编号 | 测试方法 | 用例描述 | 状态 |
|----------|----------|----------|------|
| T01 | CreateAsync_CreatesWorkflow_WithTimestamps | 创建工作流时设置时间戳 | ✅ PASS |
| T02 | GetByProjectAsync_ReturnsFilteredWorkflows | 按项目ID过滤工作流 | ✅ PASS |
| T03 | UpdateAsync_UpdatesWorkflow | 更新工作流名称 | ✅ PASS |
| T04 | DeleteAsync_DeletesWorkflow | 删除工作流 | ✅ PASS |
| T05 | CreateDefaultAsync_CreatesWithIncrementalOrder | 创建默认工作流时自动递增 Order | ✅ PASS |
| T06 | GetByIdAsync_ReturnsNewWorkflow_WhenNotFound | 查询不存在的工作流 | ✅ PASS |

## 测试统计
- **总计**: 6 个测试用例
- **通过**: 6
- **失败**: 0
- **覆盖率**: ~80% 业务逻辑覆盖

## 技术说明
- 使用 Entity Framework In-Memory Database
- 测试 Order 递增逻辑和项目过滤
