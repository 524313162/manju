# 核心接口规范

> **基于需求版本**：docs/req-v20260708/
> **关联设计文档**：[design.md](design.md)

---

## 1. 统一响应格式

### 1.1 成功响应

```json
{
  "success": true,
  "data": { /* 业务数据 */ },
  "message": null
}
```

### 1.2 失败响应

```json
{
  "success": false,
  "data": null,
  "message": "错误描述"
}
```

### 1.3 超时（promptId 返回）

```json
{
  "success": false,
  "data": {
    "promptId": "a1b2c3d4e5",
    "message": "生成中，请点击'获取资产'继续查询"
  }
}
```

---

## 2. API 端点详情

### 2.1 统一入口

| 模块 | 接口路径 | 方法 | 功能 |
|------|----------|------|------|
| LLM | `POST /api/v1/ai/chat` | JSON | 大语言模型文本生成 |
| 文生图 | `POST /api/v1/ai/image/generate` | JSON | AI 文生图 |
| 人物档案 | `POST /api/v1/ai/image/character-profile` | JSON | AI 人物档案图生成 |
| 分镜图 | `POST /api/v1/ai/storyboard/generate` | JSON | AI 分镜图生成 |
| 文生视频 | `POST /api/v1/ai/video/text-to-video` | JSON | AI 文生视频 |
| 图生视频 | `POST /api/v1/ai/video/image-to-video` | JSON | AI 图生视频 |
| BGM | `POST /api/v1/ai/bgm/generate` | JSON | AI 稳定 BGM 生成 |
| ACE 音乐 | `POST /api/v1/ai/music/compose` | JSON | AI ACE 音乐生成 |
| 查询结果 | `GET /api/v1/ai/result/{promptId}` | GET | 查询超时任务结果 |

---

### 2.2 接口请求/响应详表

#### POST /api/v1/ai/chat

AI 文本生成（LLM）

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `providerId` | long | 是 | API 提供者 ID |
| `systemPrompt` | string | 否 | 系统提示词 |
| `userPrompt` | string | 是 | 用户提示词 |

**响应**：
```json
{
  "success": true,
  "data": {
    "data": "AI 生成的文本内容",
    "message": null
  }
}
```

---

#### POST /api/v1/ai/image/generate

AI 文生图

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `prompt` | string | 是 | 图片描述提示词 |
| `negativePrompt` | string | 否 | 反向提示词 |
| `width` | int? | 否 | 图片宽度，默认 1024 |
| `height` | int? | 否 | 图片高度，默认 768 |
| `seed` | long? | 否 | 随机种子 |
| `providerId` | long? | 否 | 指定 provider（超过 0 时使用） |

**响应**：
```json
{
  "success": true,
  "data": {
    "resultUrl": "http://.../view?filename=...",
    "message": null
  }
}
```
或超时：
```json
{
  "success": false,
  "data": {
    "promptId": "a1b2c3d4e5",
    "message": "生成中，请点击'获取资产'继续查询"
  }
}
```

---

#### POST /api/v1/ai/image/character-profile

AI 人物档案图生成

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `systemPrompt` | string | 是 | 系统提示词 |
| `characterPrompt` | string | 是 | 人物提示词 |
| `negativePrompt` | string | 是 | 反向提示词 |
| `width` | int? | 否 | 默认 1792 |
| `height` | int? | 否 | 默认 1024 |
| `providerId` | long? | 否 | 指定 provider |

**响应**：同 `/image/generate`

---

#### POST /api/v1/ai/storyboard/generate

AI 分镜图生成

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `prompt` | string | 是 | 分镜描述提示词 |
| `imagePath` | string? | 否 | 参考图片路径 |
| `providerId` | long? | 否 | 指定 provider |

**响应**：同 `/image/generate`

---

#### POST /api/v1/ai/video/text-to-video

AI 文生视频

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `prompt` | string | 是 | 视频描述提示词 |
| `width` | int? | 否 | 默认 1280 |
| `height` | int? | 否 | 默认 720 |
| `duration` | int? | 否 | 默认 5 秒 |
| `fps` | int? | 否 | 默认 25 |
| `providerId` | long? | 否 | 指定 provider |

**响应**：同 `/image/generate`，`resultUrl` 为视频 URL

---

#### POST /api/v1/ai/video/image-to-video

AI 图生视频

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `imagePath` | string | 是 | 起始图片路径 |
| `prompt` | string | 是 | 视频描述提示词 |
| `width` | int? | 否 | 默认 1280 |
| `height` | int? | 否 | 默认 720 |
| `duration` | int? | 否 | 默认 3 秒 |
| `fps` | int? | 否 | 默认 25 |
| `providerId` | long? | 否 | 指定 provider |

**响应**：同 `/image/generate`

---

#### POST /api/v1/ai/bgm/generate

AI 稳定 BGM 生成

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `prompt` | string | 是 | 音乐描述 |
| `duration` | float? | 否 | 时长（秒），默认 150 |
| `providerId` | long? | 否 | 指定 provider |

**响应**：同 `/image/generate`，`resultUrl` 为音频 URL

---

#### POST /api/v1/ai/music/compose

AI ACE 音乐生成

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `prompt` | string | 是 | 音乐风格描述 |
| `lyrics` | string | 是 | 歌词 |
| `bpm` | int? | 否 | 默认 88 |
| `timesignature` | string? | 否 | 默认 "4" |
| `language` | string? | 否 | 默认 "zh" |
| `keyscale` | string? | 否 | 默认 "E minor" |
| `seconds` | double? | 否 | 时长（秒） |
| `providerId` | long? | 否 | 指定 provider |

**响应**：同 `/bgm/generate`

---

#### GET /api/v1/ai/result/{promptId}

查询超时任务的结果

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `promptId` | string | 是 | 任务 ID |
| `workflowType` | string | 是 | 工作流类型 |

**响应**：
```json
{
  "success": true,
  "data": {
    "outputs": {
      "imageUrls": ["http://..."],
      "videoUrls": ["http://..."],
      "audioUrls": ["http://..."],
      "text": "...的文本"
    }
  }
}
```
