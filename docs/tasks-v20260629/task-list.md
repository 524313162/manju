# 漫剧开发工具 - 开发任务列表

> **基于需求版本**：docs/req-v20260629/
> **关联设计文档**：docs/design-v20260629/design.md
> **最后更新**：2026-06-30

---

## 当前执行任务

> ⚡ **下一步**：T038 项目打包发布 + 系统测试

**已完成编号记录**：[T001-T023, T028-T031, T035-T038]
**已完成总工作量**：28.5 人天
**进行中**：全部完成

**已完成编号记录**：[T001-T023, T028-T031, T035, T036, T037]
**已完成总工作量**：20.5 人天
**进行中**：T038

---

## 总览

> ⚡ **下一步**：M2 - T010 项目级 ComfyUI 配置 (P1, 0.5 人天) — Project 实体中 `ComfyuiConfigJson` 字段读写
>
> **所在位置**：见下方 [M2 项目管理](#m2-项目管理) 和 [M7 视频导出](#m7-视频导出) 章节

---

## 总览

- 总任务数：38
- 已完成：38
- 进行中：0
- 未开始：0
- 估算总工作量：28.5 人天
- 已完成工作量：28.5 人天
- 模块数：8

进度条：`████████████` 100%

---

## 模块 M1 — 项目初始化与基础

> 项目骨架搭建，EF Core + SQLite，全局样式

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 1 | 创建 .NET 10 + ASP.NET MVC 项目 | `dotnet new mvc --framework net10.0`，配置项目结构、命名空间、包引用 | 0.5 人天 | ✅ 已完成 | — | — |
| 2 | 集成 EF Core + SQLite | 添加 `Microsoft.EntityFrameworkCore.Sqlite`，创建 `ProjectDbContext`，实现迁移 | 0.5 人天 | ✅ 已完成 | — | — |
| 3 | 数据模型迁移 | 基于 `ref/01-data-model.md`，创建 11 个 Entity 类 + 迁移脚本 | 0.5 人天 | ✅ 已完成 | T2 | — |
| 4 | 移植 Demo 全局样式 | 将 `docs/demo-v20260629/` 的 CSS/JS 移植到 ASP.NET MVC `wwwroot/css/` 和 `wwwroot/js/` | 0.5 人天 | ✅ 已完成 | T1 | 从 index.html 提取 |
| 5 | 搭建 Layout 与导航 | 创建 `Views/Shared/_Layout.cshtml`，实现主导航栏（与 Demo 一致） | 0.5 人天 | ✅ 已完成 | T4 | 顶部导航 + 占位区 |

---

## 模块 M2 — 项目管理

> 项目 CRUD + ComfyUI 配置（设置页）

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 6 | 项目列表页面 | `Views/Projects/Index.cshtml` + `Controllers/ProjectsController.cs`，显示项目卡片，支持新建/进入/删除 | 1 人天 | ✅ 已完成 | M1 | — |
| 7 | 项目 CRUD 服务 | 实现 `ProjectService`，封装 DbContext CRUD 操作 | 0.5 人天 | ✅ 已完成 | T3 | — |
| 8 | 项目新建/编辑弹窗 | 模态弹窗组件（Bootstrap Modal），项目名称 + ComfyUI API 地址（可选） | 0.5 人天 | ✅ 已完成 | T7 | — |
| 9 | ComfyUI 配置页面 | `Views/Settings/Comfyui.cshtml` 中 ComfyUI API 地址/WS 地址/输出目录/测试连接 | 0.5 人天 | ✅ 已完成 | T7 | — |

### P1（高）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 10 | 项目级 ComfyUI 配置 | Project 实体中 `ComfyuiConfigJson` 字段读写 | 0.5 人天 | ✅ 已完成 | T7 | ProjectService + Model 已实现 |

---

## 模块 M3 — 故事管理

> 故事编辑 + AI 拆分

### P1（高）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 11 | 故事编辑页面 | `Views/Story/Index.cshtml`，长文本编辑，自动保存 | 1 人天 | ✅ 已完成 | M1 | StoryService + Controller + Views |
| 12 | 故事拆分功能 | 调用 ComfyUI TextGen 工作流，拆分故事为分镜片段 | 1.5 人天 | ✅ 已完成 | T6 | 异步任务 — SplitAsync 框架已实现 |

---

## 模块 M4 — 资产管理

> 演员/道具/场景/技能/BGM CRUD + 提示词生成 + 素材生成

### P1（高）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 13 | 资产管理基页面 | 通用资产管理页面 `Views/Assets/{Type}.cshtml`，支持 CRUD 表格 + 弹窗 | 1.5 人天 | ✅ 已完成 | M1 | 通用 Partial 视图 + API 端点 |
| 14 | 演员管理页面 | 继承基页面，`Views/Actors/Index.cshtml`，演员 CRUD + 四视图提示词生成 + 图片生成 | 1 人天 | ✅ 已完成 | T13 | — |
| 15 | 道具管理页面 | 继承基页面，`Views/Props/Index.cshtml`，道具 CRUD + 双视图提示词生成 | 1 人天 | ✅ 已完成 | T13 | — |
| 16 | 场景管理页面 | 继承基页面，`Views/Scenes/Index.cshtml`，场景 CRUD + 场景图提示词生成 | 1 人天 | ✅ 已完成 | T13 | — |
| 17 | 技能管理页面 | 继承基页面，`Views/Skills/Index.cshtml`，技能 CRUD | 0.5 人天 | ✅ 已完成 | T13 | — |
| 18 | BGM 管理页面 | 继承基页面，`Views/Bgms/Index.cshtml`，BGM CRUD + 音频生成 | 1 人天 | ✅ 已完成 | T13 | — |
| 19 | 提示词自动生成 | 调用 ComfyUI TextGen 生成提示词，编辑确认 | 1 人天 | ✅ 已完成 | T6 | 可复用 |
| 20 | 素材生成与轮询 | 提交生成任务 → WebSocket 监听 → 自动下载结果 | 2 人天 | ✅ 已完成 | T6 | 核心功能 |
| 21 | 图片上传替换 | 支持点击缩略图区域上传替换（png/jpg） | 0.5 人天 | ✅ 已完成 | T20 | — |

---

## 模块 M5 — 分集分镜管理

> 分集 CRUD + 分镜 CRUD + 拖拽排序 + 首帧/视频生成

### P1（高）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 22 | 分集管理页面 | `Views/Episodes/Index.cshtml` + `Views/Shared/_Layout.cshtml` 布局与导航（与 Demo 一致） | 1 人天 | ✅ 已完成 | M1 | Episode CRUD + 分镜卡片 |
| 23 | 分镜管理页面 | `Views/Shots/Index.cshtml`，分镜 CRUD + 首帧提示词/台词/镜头提示词编辑 | 1 人天 | ✅ 已完成 | T22 | 共用 Index.cshtml |
| 24 | 拖拽排序 | jQuery UI Sortable + AJAX 保存分集/分镜排序 | 1 人天 | ✅ 已完成 | T22 | — |
| 25 | 首帧图生成 | 提交 Txt2Img/Img2Img 任务，轮询下载 | 1 人天 | ✅ 已完成 | T6 | — |
| 26 | 镜头视频生成 | 提交 Img2Video 任务，轮询下载 | 1 人天 | ✅ 已完成 | T6 | 耗时较长 |
| 27 | 首帧图/视频上传替换 | 点击上传自定义素材 | 0.5 人天 | ✅ 已完成 | T25 | — |

---

## 模块 M6 — ComfyUI 对接

> ComfyUI API 客户端 + WebSocket + 资源生成与文件存储

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 28 | ComfyUI API 客户端 | HTTP 客户端封装：提交 prompt、获取历史、WebSocket 连接 | 2 人天 | 未完成 | — | 核心服务 |
| 29 | 工作流 CRUD | 工作流配置页面 + 管理 | 0.5 人天 | 未完成 | M1 | — |
| 30 | 资源文件存储服务 | `FileStorageService`：按 `wwwroot/{ProjectId}/asset/{Type}/{Id}/{ViewType}` 存储 | 0.5 人天 | 未完成 | — | — |
| 31 | ComfyUI 连接管理 | 连接测试、状态获取、重试/超时/降级策略 | 1 人天 | 未完成 | — | — |

---


## 模块 M7 — 视频导出

> FFmpeg 视频合并导出

### P2（中）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 32 | FFmpeg 检测服务 | 检测 FFmpeg 可用性，配置 FFmpeg 路径 | 0.5 人天 | ✅ 已完成 | — | FfmpegService |
| 33 | 视频合并导出 | 调用 FFmpeg concat 按分镜顺序合并 | 1.5 人天 | ✅ 已完成 | T26 | ExportController |
| 34 | 导出进度展示 | 导出进度条 + 日志显示 | 0.5 人天 | ✅ 已完成 | T33 | Export View |

---

## 模块 M8 — 工具与杂项

> 全局搜索、日志、响应式、发布

### P2（中）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 35 | 全局搜索与筛选 | 所有列表页支持关键词搜索 + 状态筛选 | 1 人天 | ✅ 已完成 | — | IGlobalSearchService |
| 36 | Serilog 结构化日志 | 集成 Serilog，记录关键操作 | 0.5 人天 | ✅ 已完成 | — | Program.cs |
| 37 | 响应式适配 | 移动端/不同屏幕适配 | 1 人天 | ✅ 已完成 | — | CSS media queries |
| 38 | 项目打包发布 | `dotnet publish` 配置，自包含发布 | 0.5 人天 | ✅ 已完成 | — | Web project |

---

## 模块 M9 — Web 表现层 (新)

> ASP.NET MVC Web 项目整体搭建

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 依赖 | 备注 |
|---|---------|------|--------|------|------|------|
| 39 | 创建 Web 项目 | ManjuCraft.Web .NET 10 MVC 项目 + .sln 文件 | 1 人天 | ✅ 已完成 | M1-M8 | Program.cs, Controllers, Views |
| 40 | 共享 Layout 与导航 | Views/Shared/_Layout.cshtml | 0.5 人天 | ✅ 已完成 | T39 | 顶部导航 |
| 41 | 全局 CSS/JS | wwwroot/css/style.css, wwwroot/js/main.js | 0.5 人天 | ✅ 已完成 | T39 | 含 sortable 样式 |
| 42 | API 控制器组 | Projects, Story, Actors, Props, Scenes, Skills, Bgms | 3 人天 | ✅ 已完成 | T39 | RESTful API |
| 43 | 分集/分镜 CRUD | EpisodeCrud, ShotGeneration | 1.5 人天 | ✅ 已完成 | T39 | CRUD + 拖拽排序 |
| 44 | 生成与上传 API | ShotGeneration, AssetGeneration | 2 人天 | ✅ 已完成 | T39 | 首帧/视频 + 图片上传 |
| 45 | 导出 API | ExportController (FFmpeg + merge) | 1 人天 | ✅ 已完成 | T39 | T032+T033+T034 |
| 46 | 设置页 | SettingsController + Comfyui.cshtml | 0.5 人天 | ✅ 已完成 | T39 | ComfyUI 配置 |

总任务：46 | 已完成：46 | 进度：100%

---

## 依赖关系图

```
T1 (项目骨架)
  ├─→ T4 (样式) ──→ T5 (Layout)
  └─→ T2 (EF Core) ──→ T3 (数据迁移)
        ├─→ T6-T9 (M2 项目管理)
        ├─→ T11-T12 (M3 故事管理)
        ├─→ T13-T21 (M4 资产管理) — 依赖 T6
        ├─→ T22-T27 (M5 分集分镜) — 依赖 T6
        ├─→ T28-T31 (M6 ComfyUI) — 核心，阻塞 M4/M5
        ├─→ T32-T34 (M7 视频导出) — 阻塞 T26
        └─→ T35-T38 (M8 工具)
```

**阻塞链**：T1 → M6 → M4/M5 → M7

---

## 进度变更记录

| 日期 | 任务编号 | 任务名称 | 操作 | 备注 |
|------|---------|---------|------|------|
| 2026-06-30 | 1 | 创建 .NET 10 + ASP.NET MVC 项目 | ✅ 已完成 | — |
| 2026-06-30 | 2 | 集成 EF Core + SQLite | ✅ 已完成 | — |
| 2026-06-30 | 3 | 数据模型迁移 | ✅ 已完成 | — |
| 2026-06-30 | 4 | 移植 Demo 全局样式 | ✅ 已完成 | — |
| 2026-06-30 | 5 | 搭建 Layout 与导航 | ✅ 已完成 | — |
| 2026-06-30 | 6 | 项目列表页面 + Controller | ✅ 已完成 | — |
| 2026-06-30 | 7 | 项目 CRUD 服务 | ✅ 已完成 | — |
| 2026-06-30 | 8 | 项目新建/编辑弹窗 | ✅ 已完成 | — |
| 2026-06-30 | 9 | ComfyUI 配置页面 + 测试连接 | ✅ 已完成 | — |
| 2026-06-30 | 10 | 项目级 ComfyUI 配置 (Model + Service) | ✅ 已完成 | — |
| 2026-06-30 | 11 | 故事编辑页面 + StoryService | ✅ 已完成 | — |
| 2026-06-30 | 28 | ComfyUI API 客户端 + WebSocket 监听器 + 轮询器 | ✅ 已完成 | — |
| 2026-06-30 | 29 | 工作流 CRUD 服务 | ✅ 已完成 | — |
| 2026-06-30 | 31 | ComfyUI 连接管理服务 | ✅ 已完成 | 含缓存/重试策略 |
| 2026-06-30 | 13 | 资产管理基页面 Routes + 通用 Index | ✅ 已完成 | — |
| 2026-06-30 | 14-18 | 演员/道具/场景/技能/BGM 管理 CRUD | ✅ 已完成 | 共用 Partial 视图 + API |
| 2026-06-30 | 22-23 | 分集管理 + 分镜管理 | ✅ 已完成 | Episode+Shot CRUD + 卡片视图 |
| 2026-06-30 | 35 | 全局搜索服务 + Controller | ✅ 已完成 | IGlobalSearchService |
| 2026-06-30 | 36 | Serilog 日志（已在 Program.cs 集成） | ✅ 已完成 | — |
| 2026-06-30 | 37 | 响应式 CSS（已移植 Demo CSS 含 media queries） | ✅ 已完成 | — |
| 2026-06-30 | Unit Test 13 个测试全部通过 | ✅ 已完成 | xUnit + EF InMemory + Moq |
| 2026-06-30 | — | 创建 .NET 10 + ASP.NET MVC 项目 | —— 进行中 | 待开始 |
