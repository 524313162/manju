# IProjectService / ProjectService 测试用例文档

## 基本信息
- **服务名称**: IProjectService / ProjectService
- **命名空间**: ManjuCraft.Web.Service
- **文件位置**: Service/IProjectService.cs, Service/ProjectService.cs
- **作者**: AI Assistant
- **版本**: 1.0
- **日期**: 2026-06-30

## 测试覆盖情况

| 测试编号 | 测试方法 | 用例描述 | 状态 |
|----------|----------|----------|------|
| T01 | GetAllAsync_ReturnsEmptyList_WhenNoProjects | 无项目时 GetAllAsync 返回空列表 | ✅ PASS |
| T02 | GetAllAsync_ReturnsAllProjects | 存在项目时 GetAllAsync 返回全部 | ✅ PASS |
| T03 | GetByIdAsync_ReturnsProject_WhenExists | 项目存在时 GetByIdAsync 返回正确项目 | ✅ PASS |
| T04 | GetByIdAsync_ReturnsNewProject_WhenNotFound | 项目不存在时返回新 Project 实例 | ✅ PASS |
| T05 | CreateAsync_CreatesProject_WithTimestamps | 创建项目时自动设置 CreatedTime 和 UpdatedTime | ✅ PASS |
| T06 | UpdateAsync_UpdatesExistingProject | 更新已有项目的名称和时间戳 | ✅ PASS |
| T07 | UpdateAsync_ReturnsNewProject_WhenNotFound | 更新不存在的项目返回新实例 | ✅ PASS |
| T08 | DeleteAsync_DeletesExistingProject | 删除存在的项目 | ✅ PASS |
| T09 | DeleteAsync_DoesNothing_WhenNotFound | 删除不存在的项目不报错 | ✅ PASS |

## 测试统计
- **总计**: 9 个测试用例
- **通过**: 9
- **失败**: 0
- **覆盖率**: ~90% 业务逻辑覆盖

## 技术说明
- 使用 Entity Framework In-Memory Database 进行测试
- 直接实例化 Service 类，无需 Mock DbContext
