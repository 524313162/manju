# ComfyUI 代理服务 (ComfyuiProxy.Web)

## 项目概述

ComfyUI 代理服务提供统一的 Web API 接口，通过加载 ComfyUI 工作流模板、注入参数并自动封装为 `{ "prompt": { nodes... } }` 格式提交执行，支持多种生成任务。

## API 列表

所有接口均为 **POST**，返回 `ComfyUIResponseBase` 派生类型。

### 基础响应字段（所有接口共有）

| 字段 | 类型 | 说明 |
|------|------|------|
| `promptId` | string | ComfyUI 提示词 ID |
| `success` | bool | 是否成功 |
| `error` | string? | 错误信息（失败时） |
| `executionTimeMs` | number | 执行耗时（毫秒） |

---

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

响应额外字段：`imageUrls: string[]`

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

响应额外字段：`imageUrls: string[]`

---

### 3. 文生视频 (LTX)

**`POST /api/comfyui/ltx/text-to-video`**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 提示词 |

响应额外字段：`videoUrls: string[]`

---

### 4. 图生视频 (LTX)

**`POST /api/comfyui/ltx/image-to-video`**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `imagePath` | string | 是 | - | 起始图片路径 |
| `prompt` | string | 是 | - | 提示词 |

响应额外字段：`videoUrls: string[]`

---

### 5. 分镜生成 (HiDream)

**`POST /api/comfyui/hidream/storyboard`**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | 是 | - | 提示词 |
| `imagePath` | string? | 否 | null | 参考图片路径（可选） |

响应额外字段：`imageUrls: string[]`

---

### 6. 音乐生成 (ACE-MUSIC)

**`POST /api/comfyui/ace-music/compose`**

请求示例：
```json
{
  "prompt": "深夜氛围感慢拍，80 拍，厚重闷音 808 贝斯，暗调湿润合成音效，清冷原生颗粒沙哑女主唱，微弱回声衬音，暗光居家混音，静谧小众氛围感，松弛气声低语，轻脆击弦贝斯，饱满低频基底，冷感电影叙事质感，无人工打磨 AI 机械人声",
  "lyrics": "[Verse 1]\n窗台月色落满旧相框\n你说温柔能抵过风浪\n..." 
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
| `seconds` | double? | 否 | null | 生成时长（秒），不传则根据 BPM 和歌词字数自动计算 |

时长计算公式：`seconds = (歌词字数 / (BPM × 2)) × 60`

响应额外字段：`audioUrls: string[]`

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

响应额外字段：`audioUrls: string[]`

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

响应额外字段：`text: string`
