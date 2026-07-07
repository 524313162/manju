# ComfyUI 代理服务 (ComfyuiProxy.Web)

## 项目概述

ComfyUI 代理服务提供统一的 Web API 接口，通过加载 ComfyUI 工作流模板、注入参数并自动封装为 `{ "prompt": { nodes... } }` 格式提交执行，支持多种生成任务。

## 工作流程

提交工作流后立即返回 `promptId`，不等待执行完成，由外部轮询获取结果：

1. **`POST` 对应生成接口** — 提交工作流，返回 `{ promptId, workflowType }`
2. **`GET /api/comfyui/result/{promptId}?workflowType=xxx`** — 轮询查询结果
3. **`POST /api/comfyui/interrupt`** — 中断当前正在执行的任务
4. **`POST /api/comfyui/queue/delete`** — 删除队列中等待的任务
5. **`DELETE /api/comfyui/history/{promptId}`** — 清除执行记录

## 通用管理接口

### 查询结果

**`GET /api/comfyui/result/{promptId}?workflowType=xxx`**

- 任务未完成：`{ "success": false, "error": "未找到结果，任务可能还在执行中" }`
- 任务完成：`{ "success": true, "promptId": "...", "outputs": { ... } }`

`workflowType` 为提交时返回的值。

### 中断当前任务

**`POST /api/comfyui/interrupt`**

中断 ComfyUI 当前正在 GPU 上运行的任务。不需要 promptId。

### 删除队列任务

**`POST /api/comfyui/queue/delete`**

```json
{ "promptIds": ["prompt_id_1", "prompt_id_2"] }
```

删除还在排队等待的任务（已在运行的不会被删除）。

### 删除历史记录

**`DELETE /api/comfyui/history/{promptId}`**

删除执行记录，不影响已生成的输出文件。

### 上传图片

**`POST /api/comfyui/upload`** (Content-Type: `multipart/form-data`)

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `image` | file | 是 | 图片文件 |
| `subfolder` | string | 否 | ComfyUI 子目录，留空为根目录 |

响应（ComfyUI 原样返回）：
```json
{ "name": "my_image.png", "subfolder": "", "type": "input" }
```

---

## 生成接口

所有生成接口均为 **POST**，返回 `{ promptId: string, workflowType: string }`。

### 1. 文生图 (ZIMAGE)

**`POST /api/comfyui/zimage/text-to-image`**

请求示例：
```json
{
  "prompt": "一个8岁的小女孩，戴草帽，在田间里开心的笑",
  "width": 1024,
  "height": 768
}
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 正向提示词 |
| `width` | int | 否 | 1024 | 图像宽度（≤0 时使用默认值） |
| `height` | int | 否 | 768 | 图像高度（≤0 时使用默认值） |

`workflowType`：`zimage-text-to-image`

---

### 2. 人物档案 (ZIMAGE)

**`POST /api/comfyui/zimage/character-profile`**

请求示例：
```json
{
  "systemPrompt": "17 岁少年真人男生",
  "characterPrompt": "人物三视图",
  "negativePrompt": "裸露、色情、畸形人体"
}
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `systemPrompt` | string | 是 | - | 系统提示词（版面/风格描述） |
| `characterPrompt` | string | 是 | - | 人物提示词 |
| `negativePrompt` | string | 是 | - | 反向提示词 |
| `width` | int | 否 | 1792 | 生成宽度（≤0 时使用默认值） |
| `height` | int | 否 | 1024 | 生成高度（≤0 时使用默认值） |

`workflowType`：`zimage-character-profile`

---

### 3. 文生视频 (LTX)

**`POST /api/comfyui/ltx/text-to-video`**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 提示词 |
| `width` | int | 否 | 1280 | 视频宽度 |
| `height` | int | 否 | 720 | 视频高度 |
| `duration` | int | 否 | 5 | 时长（秒） |
| `fps` | int | 否 | 25 | 帧率 |

`workflowType`：`ltx-text-to-video`

---

### 4. 图生视频 (LTX)

**`POST /api/comfyui/ltx/image-to-video`**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `imagePath` | string | 是 | - | 已上传的文件名（通过上传接口获取） |
| `prompt` | string | 否 | "" | 提示词 |
| `width` | int | 否 | 1280 | 视频宽度 |
| `height` | int | 否 | 720 | 视频高度 |
| `duration` | int | 否 | 3 | 时长（秒） |
| `fps` | int | 否 | 25 | 帧率 |

`workflowType`：`ltx-image-to-video`

---

### 5. 分镜生成 (HiDream)

**`POST /api/comfyui/hidream/storyboard`**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 提示词 |
| `imagePath` | string? | 否 | null | 参考图片路径（可选） |

`workflowType`：`hidream-storyboard`

---

### 6. 音乐生成 (ACE-MUSIC)

**`POST /api/comfyui/ace-music/compose`**

请求示例：
```json
{
  "prompt": "深夜氛围感慢拍，80 拍，厚重闷音 808 贝斯...",
  "lyrics": "[Verse 1]\n窗台月色落满旧相框\n..."
}
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 音乐风格/描述 |
| `lyrics` | string | 是 | - | 歌词 |
| `bpm` | int | 否 | 88 | 每分钟节拍数 |
| `timesignature` | string | 否 | "4" | 拍号（如 "4" 表示 4/4 拍） |
| `language` | string | 否 | "zh" | 歌词语言 |
| `keyscale` | string | 否 | "E minor" | 调式/音阶 |
| `seconds` | double? | 否 | null | 生成时长（秒），不传则自动计算 |

时长公式：`seconds = (歌词字数 / (BPM × 2)) × 60`

`workflowType`：`ace-music-compose`

---

### 7. BGM 生成 (STABLE-BGM)

**`POST /api/comfyui/stable-bgm/generate`**

请求示例：
```json
{
  "prompt": "一段旋律虐人的爱情BGM，影视剧中的虐人桥段",
  "duration": 30
}
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 音乐描述 |
| `duration` | float? | 否 | 150 | 音频时长（秒） |

`workflowType`：`stable-bgm-generate`

---

### 8. LLM 文本生成 (QWEN)

**`POST /api/comfyui/llm-qwen/execute`**

请求示例：
```json
{
  "prompt": "你叫什么名字",
  "maxLength": 2048
}
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 提示词 |
| `maxLength` | int? | 否 | 2048 | 最大生成长度 |

`workflowType`：`llm-qwen-execute`
