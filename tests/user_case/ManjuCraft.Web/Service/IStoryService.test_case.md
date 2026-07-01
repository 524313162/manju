# IStoryService / StoryService 测试用例文档

## 基本信息
- **服务名称**: IStoryService / StoryService
- **命名空间**: ManjuCraft.Web.Service
- **文件位置**: Service/IStoryService.cs, Service/StoryService.cs
- **作者**: AI Assistant
- **版本**: 1.0
- **日期**: 2026-06-30

## 测试覆盖情况

| 测试编号 | 测试方法 | 用例描述 | 状态 |
|----------|----------|----------|------|
| T01 | CreateAsync_CreatesStory_WithTimestamps | 创建故事时自动设置时间戳 | ✅ PASS |
| T02 | GetByProjectIdAsync_ReturnsFilteredStories | 按项目ID过滤故事列表 | ✅ PASS |
| T03 | GetByIdAsync_ReturnsStory_WhenExists | 故事存在时返回正确数据 | ✅ PASS |
| T04 | UpdateAsync_UpdatesStory | 更新故事内容 | ✅ PASS |
| T05 | DeleteAsync_DeletesStory | 删除故事 | ✅ PASS |
| T06 | SplitAsync_UpdatesSplitContent | 拆分故事内容 | ✅ PASS |
| T07 | SplitAsync_ReturnsNewStory_WhenNotFound | 拆分不存在的故事返回新实例 | ✅ PASS |

## 测试统计
- **总计**: 7 个测试用例
- **通过**: 7
- **失败**: 0
- **覆盖率**: ~85% 业务逻辑覆盖

## 技术说明
- 使用 Entity Framework In-Memory Database
- 测试项目间隔离性和故事拆分功能
