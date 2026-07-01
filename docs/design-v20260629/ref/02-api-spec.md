# 漫剧开发工具 - API 接口设计规范

> **基于需求版本**：docs/req-v20260629/
> **关联设计文档**：[design.md](design.md)

---

## 1. API 设计原则

### 1.1 资源命名规范

| 规则 | 正确示例 | 错误示例 |
|------|---------|---------|
| 使用名词复数形式 | `/api/v1/projects` | `/api/v1/getProject` |
| 使用小写字母和连字符 | `/api/v1/story-content` | `/api/v1/storyContent` |
| 层级关系用路径表达 | `/api/v1/projects/{id}/actors` | `/api/v1/getActors?projectId=` |
| 不在路径中使用动词 | `DELETE /api/v1/projects/{id}` | `POST /api/v1/deleteProject` |
| 版本号放在路径前缀 | `/api/v1/projects` | `/api/projects/v1` |

### 1.2 HTTP 方法语义

| HTTP 方法 | 语义 | 示例 |
|-----------|------|------|
| GET | 查询资源 | `GET /api/v1/projects/{id}` |
| POST | 创建新资源 | `POST /api/v1/projects` |
| PUT | 全量更新资源 | `PUT /api/v1/projects/{id}` |
| DELETE | 删除资源 | `DELETE /api/v1/projects/{id}` |
| PATCH | 部分更新资源 | `PATCH /api/v1/projects/{id}` |

### 1.3 版本控制

- 所有 API 路径以 `/api/v1/` 为前缀

---

## 2. 统一响应格式

### 2.1 成功响应

**单个资源**：
```json
{
  "success": true,
  "data": { "id": 1, "name": "示例项目" },
  "message": null,
  "timestamp": "2026-06-29T04:00:00Z"
}
```

**列表响应**：
```json
{
  "success": true,
  "data": {
    "items": [{ "id": 1, "name": "..." }],
    "total": 100,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "message": null,
  "timestamp": "2026-06-29T04:00:00Z"
}
```

**异步任务响应**：
```json
{
  "success": true,
  "data": {
    "taskId": "task-12345",
    "status": "processing",
    "message": "资源生成中，可通过 taskId 查询进度"
  },
  "message": null,
  "timestamp": "2026-06-29T04:00:00Z"
}
```

### 2.2 失败响应

```json
{
  "success": false,
  "data": null,
  "message": "请求参数无效",
  "errorCode": "VALIDATION_ERROR",
  "errors": [
    { "field": "name", "message": "项目名称不能为空" }
  ],
  "timestamp": "2026-06-29T04:00:00Z"
}
```

### 2.3 统一错误码

| 错误码 | HTTP 状态 | 说明 |
|--------|-----------|------|
| VALIDATION_ERROR | 400 | 输入参数验证失败 |
| NOT_FOUND | 404 | 资源不存在 |
| DUPLICATE_ENTRY | 422 | 数据重复 |
| COMFYUI_ERROR | 422 | ComfyUI 生成失败 |
| INTERNAL_ERROR | 500 | 系统内部错误 |

---

## 3. API 端点清单

### 3.1 项目 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects | 获取项目列表 | PagedResult<ProjectDto> |
| GET | /api/v1/projects/{id} | 获取项目详情 | ProjectDto |
| POST | /api/v1/projects | 创建项目 | ProjectDto |
| PUT | /api/v1/projects/{id} | 更新项目 | ProjectDto |
| DELETE | /api/v1/projects/{id} | 删除项目 | 204 |
| POST | /api/v1/projects/{id}/test-comfyui | 测试 ComfyUI 连接 | TestResultDto |
| GET | /api/v1/projects/{id}/comfyui-status | 获取 ComfyUI 状态 | ComfyuiStatusDto |

### 3.2 故事 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/story | 获取项目故事 | StoryDto |
| PUT | /api/v1/projects/{projectId}/story | 更新故事 | StoryDto |
| POST | /api/v1/projects/{projectId}/story/split | 拆分故事 | TaskResponseDto |
| GET | /api/v1/tasks/{taskId}/status | 获取任务状态 | TaskStatusDto |

### 3.3 演员 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/actors | 获取演员列表 | ListDto<ActorDto> |
| GET | /api/v1/actors/{id} | 获取演员详情 | ActorDto |
| POST | /api/v1/projects/{projectId}/actors | 创建演员 | ActorDto |
| PUT | /api/v1/actors/{id} | 更新演员 | ActorDto |
| DELETE | /api/v1/actors/{id} | 删除演员 | 204 |
| GET | /api/v1/actors/{id}/images | 获取四视图资源 | ListDto<EntityImageDto> |
| POST | /api/v1/actors/{id}/generate-prompt | 生成提示词 | PromptResponseDto |
| POST | /api/v1/actors/{id}/generate-images | 生成四视图图片 | TaskResponseDto |
| POST | /api/v1/actors/{id}/images/upload | 上传替换图片 | EntityImageDto |

### 3.4 道具 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/props | 获取道具列表 | ListDto<PropDto> |
| GET | /api/v1/props/{id} | 获取道具详情 | PropDto |
| POST | /api/v1/projects/{projectId}/props | 创建道具 | PropDto |
| PUT | /api/v1/props/{id} | 更新道具 | PropDto |
| DELETE | /api/v1/props/{id} | 删除道具 | 204 |
| GET | /api/v1/props/{id}/images | 获取双视图资源 | ListDto<EntityImageDto> |
| POST | /api/v1/props/{id}/generate-prompt | 生成提示词 | PromptResponseDto |
| POST | /api/v1/props/{id}/generate-images | 生成双视图 | TaskResponseDto |
| POST | /api/v1/props/{id}/images/upload | 上传替换图片 | EntityImageDto |

### 3.5 场景 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/scenes | 获取场景列表 | ListDto<SceneDto> |
| GET | /api/v1/scenes/{id} | 获取场景详情 | SceneDto |
| POST | /api/v1/projects/{projectId}/scenes | 创建场景 | SceneDto |
| PUT | /api/v1/scenes/{id} | 更新场景 | SceneDto |
| DELETE | /api/v1/scenes/{id} | 删除场景 | 204 |
| GET | /api/v1/scenes/{id}/images | 获取场景图资源 | ListDto<EntityImageDto> |
| POST | /api/v1/scenes/{id}/generate-prompt | 生成提示词 | PromptResponseDto |
| POST | /api/v1/scenes/{id}/generate-images | 生成场景图 | TaskResponseDto |

### 3.6 技能 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/skills | 获取技能列表 | ListDto<SkillDto> |
| POST | /api/v1/projects/{projectId}/skills | 创建技能 | SkillDto |
| PUT | /api/v1/skills/{id} | 更新技能 | SkillDto |
| DELETE | /api/v1/skills/{id} | 删除技能 | 204 |

### 3.7 BGM API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/bgms | 获取 BGM 列表 | ListDto<BgmDto> |
| POST | /api/v1/projects/{projectId}/bgms | 创建 BGM | BgmDto |
| PUT | /api/v1/bgms/{id} | 更新 BGM | BgmDto |
| DELETE | /api/v1/bgms/{id} | 删除 BGM | 204 |
| POST | /api/v1/bgms/{id}/generate-audio | 生成 BGM | TaskResponseDto |

### 3.8 分集 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/episodes | 获取分集列表 | ListDto_episodeDto> |
| POST | /api/v1/projects/{projectId}/episodes | 创建分集 | EpisodeDto |
| PUT | /api/v1/episodes/{id} | 更新分集 | EpisodeDto |
| DELETE | /api/v1/episodes/{id} | 删除分集 | 204 |
| PUT | /api/v1/episodes/{projectId}/reorder | 调整分集顺序 | ListDto_episodeDto> |

### 3.9 分镜 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/episodes/{episodeId}/shots | 获取分镜列表 | ListDto<ShotDto> |
| POST | /api/v1/episodes/{episodeId}/shots | 创建分镜 | ShotDto |
| PUT | /api/v1/shots/{id} | 更新分镜 | ShotDto |
| DELETE | /api/v1/shots/{id} | 删除分镜 | 204 |
| PUT | /api/v1/shots/{episodeId}/reorder | 调整分镜顺序 | ListDto<ShotDto> |
| POST | /api/v1/shots/{id}/generate-firstframe | 生成首帧图 | TaskResponseDto |
| POST | /api/v1/shots/{id}/generate-video | 生成镜头视频 | TaskResponseDto |
| POST | /api/v1/shots/{id}/images/upload | 上传替换首帧图 | EntityImageDto |
| POST | /api/v1/shots/{id}/video/upload | 上传替换视频 | EntityImageDto |

### 3.10 工作流 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/projects/{projectId}/workflows | 获取工作流列表 | ListDto<WorkflowDto> |
| POST | /api/v1/projects/{projectId}/workflows | 创���工作流 | WorkflowDto |
| PUT | /api/v1/workflows/{id} | 更新工作流 | WorkflowDto |
| DELETE | /api/v1/workflows/{id} | 删除工作流 | 204 |

### 3.11 设置 API

| 方法 | 路径 | 描述 | 响应 |
|------|------|------|------|
| GET | /api/v1/settings/comfyui | 获取 ComfyUI 配置 | ComfyuiSettingsDto |
| PUT | /api/v1/settings/comfyui | 更新 ComfyUI 配置 | 204 |
| GET | /api/v1/settings/ffmpeg | 检查 FFmpeg 状态 | FfmpegStatusDto |

---

## 4. DTO 定义

### 4.1 ProjectDto

```json
{
  "id": 1,
  "name": "我的漫剧",
  "comfyuiConfigJson": null,  // 可选，null=使用全局配置
  "createdTime": 1719600000000,
  "updatedTime": 1719600000000
}
```

### 4.2 ActorDto

```json
{
  "id": 1,
  "projectId": 1,
  "name": "主角A",
  "description": "描述...",
  "fourViewPrompt": "正面半身，...|背面...",
  "defaultWorkflowType": "Txt2Img",
  "order": 0,
  "imageCount": 4,
  "createdTime": 1719600000000,
  "updatedTime": 1719600000000
}
```

### 4.3 EpisodeDto

```json
{
  "id": 1,
  "projectId": 1,
  "name": "第一集",
  "duration": 300,
  "order": 0,
  "shotCount": 10,
  "createdTime": 1719600000000,
  "updatedTime": 1719600000000
}
```

### 4.4 ShotDto

```json
{
  "id": 1,
  "episodeId": 1,
  "firstFramePrompt": "镜头描述...",
  "firstFrameWorkflowType": "Img2Img",
  "dialog": "台词内容...",
  "videoPrompt": "视频生成提示词...",
  "videoWorkflowType": "Img2Video",
  "order": 0,
  "hasFirstFrame": false,
  "hasVideo": false,
  "createdTime": 1719600000000,
  "updatedTime": 1719600000000
}
```

### 4.5 EntityImageDto

```json
{
  "id": 1,
  "entityType": "Actor",
  "entityId": 1,
  "viewType": "Front",
  "mediaType": "Image",
  "fileUrl": "/api/v1/assets/actor/1/Front.png",
  "createdTime": 1719600000000,
  "updatedTime": 1719600000000
}
```

### 4.6 TaskResponseDto

```json
{
  "taskId": "task-abc123",
  "status": "processing",  // pending | processing | completed | failed
  "progress": 0,  // 0-100
  "message": null
}
```

### 4.7 ComfyuiSettingsDto

```json
{
  "apiUrl": "http://localhost:8188",
  "wsUrl": "ws://localhost:8188/ws"
}
```

### 4.8 FfmpegStatusDto

```json
{
  "available": true,
  "version": "6.0",
  "installed": true
}
```

---

## 5. 上传接口

### 5.1 图片上传

**POST** `/api/v1/entities/{entityType}/{entityId}/images`

- Content-Type: `multipart/form-data`
- 表单字段：`file` (图片文件), `viewType` (视图类型)
- 响应：`EntityImageDto`

### 5.2 视频上传

**POST** `/api/v1/entities/{entityType}/{entityId}/videos`

- Content-Type: `multipart/form-data`
- 表单字段：`file` (视频文件), `viewType` (视图类型)
- 响应：`EntityImageDto`

---

## 6. 认证方式

无需用户认证。所有 API 均可直接访问。
