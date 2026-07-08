# ComfyUI 代理外部系统集成接口

> **基于需求版本**：docs/req-v20260708/
> **关联设计文档**：[design.md](design.md)

---

## 1. 外部系统清单

| 系统名称 | 技术栈 | 集成方式 | 用途 |
|----------|--------|----------|------|
| ComfyuiProxy.Web | ASP.NET Core 8.0 | HTTP REST | ComfyUI 代理，封装 8 种 AI 工作流 |
| ComfyUI Server | Python/Node.js | WebSocket + HTTP | 实际 AI 工作流执行引擎 |

---

## 2. 接口规范

### 2.1 ComfyuiProxy.Web 代理接口

> 所有代理接口由 ComfyuiProxy.Web（http://{proxyUrl}）提供，ManjuCraft.Web 通过 HttpClient 调用。

#### 文本生成（LLM）

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/llm-qwen/execute | 提交 LLM 文本生成任务 | — | `{ prompt: string, max_length?: number }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=string | 查询 LLM 执行结果 | — | — | `{ success: bool, outputs: { text: string } }` |

#### 文生图（ZImage）

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/zimage/text-to-image | 提交文生图任务 | — | `{ prompt: string, width?: 1024, height?: 768 }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=zimage-text-to-image | 查询文生图结果 | — | — | `{ success: bool, outputs: { imageUrls: [string] } }` |

#### 人物档案图生成

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/zimage/character-profile | 提交人物档案图生成 | — | `{ systemPrompt, characterPrompt, negativePrompt, width?: 1792, height?: 1024 }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=zimage-character-profile | 查询人物档案图结果 | — | — | `{ success: bool, outputs: { imageUrls: [string] } }` |

#### 分镜图生成（HiDream）

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/hidream/storyboard | 提交流程图生成 | — | `{ prompt: string, imagePath?: string }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=hidream-storyboard | 查询分镜图结果 | — | — | `{ success: bool, outputs: { imageUrls: [string] } }` |

#### 文生视频（LTX）

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/ltx/text-to-video | 提交文生视频 | — | `{ prompt, width?: 1280, height?: 720, duration?: 5, fps?: 25 }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=ltx-text-to-video | 查询文生视频结果 | — | — | `{ success: bool, outputs: { videoUrls: [string] } }` |

#### 图生视频（LTX）

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/ltx/image-to-video | 提交图生视频 | — | `{ imagePath, prompt, width?: 1280, height?: 720, duration?: 3, fps?: 25 }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=ltx-image-to-video | 查询图生视频结果 | — | — | `{ success: bool, outputs: { videoUrls: [string] } }` |

#### ACE 音乐生成

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/ace-music/compose | 提交音乐生成 | — | `{ prompt, lyrics, bpm?: 88, timesignature?: "4", language?: "zh", keyscale?: "E minor", seconds? }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=ace-music-compose | 查询音乐生成结果 | — | — | `{ success: bool, outputs: { audioUrls: [string] } }` |

#### 稳定 BGM 生成

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| POST | /api/comfyui/stable-bgm/generate | 提交 BGM 生成 | — | `{ prompt, duration? }` | `{ promptId: string, workflowType: string }` |
| GET | /api/comfyui/result/{promptId}?workflowType=stable-bgm-generate | 查询 BGM 结果 | — | — | `{ success: bool, outputs: { audioUrls: [string] } }` |

#### 通用接口

| 方法 | 路径 | 描述 | 认证 | 请求体 | 响应 |
|------|------|------|------|--------|------|
| GET | /api/comfyui/result/{promptId}?workflowType=string | 通用查询结果 | — | — | `{ success, outputs }` |
| POST | /api/comfyui/interrupt | 中断当前任务 | — | — | `{ message }` |

---

## 3. 数据同步方案

- 不涉及批量数据同步
- 所有数据实时请求/响应
- 资产（图片/音频）通过 `AssetsController/ReplaceResource` 上传到本地文件系统

---

## 4. 异常处理

| 异常场景 | 降级方案 |
|----------|----------|
| ComfyUI 代理不可达 | 返回错误提示 "代理服务连接失败" |
| ComfyUI 服务器不可达 | 返回错误提示 "AI 服务不可用" |
| 任务超时（10 分钟） | 返回 promptId，前端显示提示，用户手动点击"获取资产" |
| ComfyUI 单次生成失败 | 显示具体错误信息，支持重试 |
| LLM API 不可用 | 返回错误提示，提示用户检查 API Key 配置 |
