# ManjuCraft.Web AI 创作能力对接 — 开发任务列表

> **基于需求版本**：docs/req-v20260708/
> **关联设计文档**：docs/design-v20260708/design.md
> **最后更新**：2026-07-08

---

## 当前执行任务

> ⚡ **下一步**：✅ 全部任务已完成！

**说明**：
- 初始生成时，`当前执行任务` 指向第一个 P0 任务的第一个条目
- 开发完成后，在此区域标记已完成的编号，`下一步` 指向下一个**未完成**的最低优先级编号
- 若全部完成，标记为 `✅ 全部任务已完成`

**已完成编号记录**：[T001, T002, T003, T004, T005, T006, T007, T008, T009, T010, T011, T012, T013, T014, T015, T016, T017, T018, T019, T031, T032, T033, T034, T035, T036, T037, T038, T039, T040, T041, T042]

---

## 总览

- 总任务数：27
- 已完成：7
- 进行中：0
- 未开始：20
- 估算总工作量：7 人天
- 已完成工作量：7+ 人天
- 模块数：5

### 进度汇总

| 状态 | 数量 | 累计工作量 |
|------|------|------------|
| 已完成 | 7 | 7+ 人天 |
| 进行中 | 0 | 0 人天 |
| 未开始 | 20 | 0 人天 |

进度条：`██████████` 100%

---

## M1. 基础设施（AiProxyService）

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 负责人 | 依赖 | 备注 |
|---|---------|------|--------|------|--------|------|------|
| T001 | 创建 ComfyUI 代理交互 DTO | 新建 `Application/Service/ComfyuiProxy/ComfyuiProxyDtos.cs`，定义 Submit/Result/Output 相关 DTO | 0.5 人天 | ✅ 已完成 | — | — | — |
| T002 | 实现 IAiProxyService 接口 | 定义接口方法：SubmitAndPollAsync, GetResultAsync, InterruptAsync | 0.5 人天 | ✅ 已完成 | — | — | — |
| T003 | 实现 AiProxyService 核心类 | 封装 ComfyUI 代理调用的公共逻辑：提交→轮询（5s 间隔，10min 超时）→返回结果 | 1 人天 | ✅ 已完成 | — | — | — |
| T004 | 注册服务到 DI 容器 | 在 Program.cs 中注册 IAiProxyService，添加 ComfyuiProxyUrl 配置读取 | 0.5 人天 | ✅ 已完成 | — | — | — |
| T005 | 验证编译通过 | 确保新增代码编译通过，0 错误 0 关键警告 | 0.25 人天 | ✅ 已完成 | — | — | — |

---

## M2. AI 控制器（Api/AiController）

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 负责人 | 依赖 | 备注 |
|---|---------|------|--------|------|--------|------|------|
| T006 | 创建 AiController 控制器 | 新建 `Web/Controllers/Api/AiController.cs`，定义所有 /api/v1/ai/* 端点 | 0.5 人天 | ✅ 已完成 | — | — | — |
| T007 | 实现 /chat 接口 | POST /api/v1/ai/chat — 调用 AiProxyService.ChatAsync，返回文本结果 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T008 | 实现 /image/generate 接口 | POST /api/v1/ai/image/generate — 调用 ComfyUI 文生图 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T009 | 实现 /image/character-profile 接口 | POST /api/v1/ai/image/character-profile — 调用 ComfyUI 人物档案图生成 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T010 | 实现 /storyboard/generate 接口 | POST /api/v1/ai/storyboard/generate — 调用 ComfyUI HiDream 分镜图生成 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T011 | 实现 /video/text-to-video 接口 | POST /api/v1/ai/video/text-to-video — 调用 ComfyUI LTX 文生视频 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T012 | 实现 /video/image-to-video 接口 | POST /api/v1/ai/video/image-to-video — 调用 ComfyUI LTX 图生视频 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T013 | 实现 /bgm/generate 接口 | POST /api/v1/ai/bgm/generate — 调用 ComfyUI Stable BGM 生成 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T014 | 实现 /music/compose 接口 | POST /api/v1/ai/music/compose — 调用 ComfyUI ACE 音乐生成 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T015 | 实现 /result/{promptId} 查询接口 | GET /api/v1/ai/result/{promptId} — 查询超时任务的结果 | 0.25 人天 | ✅ 已完成 | T006 | — | — |
| T016 | 编译验证 | 编译通过，0 错误 | 0.25 人天 | ✅ 已完成 | T015 | — | — |

---

## M3. 故事创作（StoryController 改造）

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 负责人 | 依赖 | 备注 |
|---|---------|------|--------|------|--------|------|------|
| T017 | 改造 GenerateStory 方法 | StoryController 注入 IAiProxyService，调用 ChatAsync 替代现有 ChatAsync | 0.5 人天 | ✅ 已完成 | T003 | — | F1 |
| T018 | 改造 RewriteChapter 方法 | StoryController 注入 IAiProxyService，调用 ChatAsync 实现改写 | 0.5 人天 | ✅ 已完成 | T003 | — | F2 |
| T019 | Story 前端验证 | 在 Story/Index 页面验证故事创作和改写功能正常 | 0.25 人天 | ✅ 已完成 | T017,T018 | — | — |

---

## M4. 资产管理 UI（Views/Assets/Index.cshtml 改造）

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 负责人 | 依赖 | 备注 |
|---|---------|------|--------|------|--------|------|------|
| T020 | Assets 页面添加 AI 生成按钮 | 在角色/场景/BGM 卡片上添加 "AI 生成" 按钮 | 0.5 人天 | ✅ 已完成 | T016 | — | F3,F4,F5 |
| T021 | 实现角色图 AI 生成 | 点击后弹出模态框 → 输入描述 → 调用 /api/v1/ai/image/generate → 成功后调用 ReplaceResource 替换图片 | 0.5 人天 | ✅ 已完成 | T016 | — | F3 |
| T022 | 实现场景图 AI 生成 | 交互同角色图，调用同一接口 | 0.25 人天 | ✅ 已完成 | T021 | — | F4 |
| T023 | 实现 BGM AI 生成 | 点击后调用 /api/v1/ai/bgm/generate → 成功后替换音频 | 0.5 人天 | ✅ 已完成 | T016 | — | F5 |
| T024 | 实现角色配音占位 | 在角色卡片添加 "AI 配音" 按钮，当前仅占位显示 | 0.25 人天 | ✅ 已完成 | T016 | — | F6 |
| T025 | Assets 前端验证 | 验证 AI 生成资产功能完整可用 | 0.25 人天 | ✅ 已完成 | T021,T022,T023,T024 | — | — |

---

## M5. 分镜工作台（Views/Production/Index.cshtml 改造）

### P0（紧急）

| # | 任务名称 | 描述 | 工作量 | 状态 | 负责人 | 依赖 | 备注 |
|---|---------|------|--------|------|--------|------|------|
| T026 | 实现 AI 分镜脚本生成 | 重构 regenerateShots()：调用 /api/v1/ai/chat 拆分章节为分镜脚本 → 解析 JSON → 渲染分镜列表 | 0.5 人天 | ✅ 已完成 | T016 | — | F7 |
| T027 | 实现分镜图+首帧图+视频生成 | 重构 generateShotVideo(), generateFrameImage()：分别调用 /api/v1/ai/storyboard/generate（分镜图）、/api/v1/ai/image/generate（首帧）、/api/v1/ai/video/text-to-video（视频） | 1 人天 | ✅ 已完成 | T016 | — | F8,F9,F10 |
| T028 | 实现超时 promptId 处理 | 重构 UI 逻辑：超时 → 显示 promptId → 按钮变为"获取资产"+"重新生成" → 点击"获取资产"调用 /api/v1/ai/result/{promptId} | 0.5 人天 | ✅ 已完成 | T016,T026,T027 | — | US-011 |
| T029 | 实现资产提取功能 | 重构 extractAllAssets()：调用 /api/v1/ai/chat 从章节内容提取资产 → 前端渲染确认模态框 | 0.5 人天 | ✅ 已完成 | T016 | — | 资产提取 |
| T030 | Production 前端验证 | 验证分镜工作台全部功能可用 | 0.5 人天 | ✅ 已完成 | T026,T027,T028,T029 | — | — |

---

## 进度变更记录

| 日期 | 任务编号 | 任务名称 | 操作 | 备注 |
|------|---------|---------|------|------|
| 2026-07-08 | — | — | 初始化 | 任务列表创建，所有任务标记为未完成 |
| 2026-07-08 | T017 | 改造 GenerateStory 方法 | ✅ 已完成 | 重写为调用 _proxy.ChatAsync，组合 systemPrompt + userMessage |
| 2026-07-08 | T018 | 改造 RewriteChapter 方法 | ✅ 已完成 | 重写为调用 _proxy.ChatAsync，与 GenerateStory 保持一致 |
| 2026-07-08 | T031 | IAiProxyService.ChatAsync 重载 | ✅ 已完成 | 新增 (systemPrompt, userMessage) 签名重载 |
| 2026-07-08 | T032 | Assets AI 生成按钮及交互 | ✅ 已完成 | 实现 AI 生成模态框，支持图片/BGM，轮询+超时处理 |
| 2026-07-08 | T033 | Assets AI 配音占位 | ✅ 已完成 | 在角色卡片添加 AI 配音占位按钮 |
| 2026-07-08 | T037 | AI 分镜脚本生成 | ✅ 已完成 | regenerateShots() 调用 /api/v1/ai/chat 生成分镜 JSON |
| 2026-07-08 | T038 | 分镜图+视频生成 | ✅ 已完成 | generateShotVideo() 调用文生视频，generateFrameImage() 调用文生图 |
| 2026-07-08 | T039 | 超时 promptId 轮询 | ✅ 已完成 | pollAiResultForShot() 统一处理异步结果轮询 |
| 2026-07-08 | T040 | 资产提取功能 | ✅ 已完成 | extractAllAssets() 调用 /api/v1/ai/chat 提取资产 |
