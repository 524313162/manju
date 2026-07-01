# IComfyuiConnectionService / ComfyuiConnectionService 测试用例文档

## 基本信息
- **服务名称**: IComfyuiConnectionService / ComfyuiConnectionService
- **命名空间**: ManjuCraft.Web.Service
- **文件位置**: Service/ComfyuiConnectionService.cs
- **作者**: AI Assistant
- **版本**: 1.0
- **日期**: 2026-06-30

## 测试覆盖情况

| 测试编号 | 测试方法 | 用例描述 | 状态 |
|----------|----------|----------|------|
| T01 | TestConnectionAsync_ReturnsClientStatus | 测试连接返回客户端状态 | ✅ PASS |
| T02 | GetSystemStatsAsync_UsesCache_WhenValid | 系统统计使用缓存机制 | ✅ PASS |
| T03 | TestConnectionAsync_CachesResult | 测试连接后缓存结果 | ✅ PASS |

## 测试统计
- **总计**: 3 个测试用例
- **通过**: 3
- **失败**: 0
- **覆盖率**: ~80% 业务逻辑覆盖（缓存 + 连接测试）

## 技术说明
- 使用 Moq 框架 Mock IComfyuiClient
- 测试连接服务和内部缓存逻辑
- 验证 HTTP 调用的最少调用次数
