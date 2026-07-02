# 漫剧开发工具 - 外部系统集成接口

> **基于需求版本**：docs/req-v20260629/ v2.0 (2026-07-02)
> **关联设计文档**：[design.md](design.md)

---

## 1. 外部系统清单

| 系统名称 | 技术栈 | 集成方式 | 用途 |
|----------|--------|----------|------|
| LLM (DeepSeek) | OpenAI 兼容 API | HTTP REST | AI 文本生成（剧本创作、清单生成、提示词生成） |
| LLM (NVIDIA) | OpenAI 兼容 API | HTTP REST | AI 文本生成 |
| LLM (Ollama) | OpenAI 兼容 API | HTTP REST | 本地 AI 文本生成 |
| ComfyUI | Python/WebSocket | HTTP API + WebSocket + REST | AI 素材生成（图片/视频/音频） |
| ffmpeg | CLI | 进程调用 | 视频合并导出 |
| 本地磁盘 | 文件系统 | 文件读写 | 素材存储、图片/视频替换 |

---

## 2. 接口规范

### 2.1 OpenAI 兼容 LLM API（DeepSeek / NVIDIA / Ollama 通用）

#### 2.1.1 文本对话

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | /chat/completions | Bearer Token | 发送聊天消息，获取文本回复 |

**请求体**：
```json
{
  "model": "deepseek-chat",
  "messages": [
    { "role": "system", "content": "系统提示词..." },
    { "role": "user", "content": "用户输入..." }
  ],
  "stream": false           // false = 完整返回, true = SSE 流式
}
```

**非流式响应**：
```json
{
  "id": "chatcmpl-xxx",
  "choices": [
    { "index": 0, "message": { "role": "assistant", "content": "AI 生成的文本内容..." }, "finish_reason": "stop" }
  ]
}
```

**流式响应（SSE）**：
```
data: {"choices":[{"delta":{"content":"片"},"finish_reason":null}]}

data: {"choices":[{"delta":{"content":"段"},"finish_reason":null}]}

data: {"choices":[{"delta":{},"finish_reason":"stop"}]}

data: [DONE]
```

#### 2.1.2 各提供商配置差异

| 提供商 | ApiUrl | Model 示例 | Note |
|--------|--------|-----------|------|
| DeepSeek | `https://api.deepseek.com` | `deepseek-chat` | 需要 Bearer Token |
| NVIDIA | 提供商指定 URL | 模型名由提供商提供 | 需要 API Key |
| Ollama | `http://localhost:11434` | `llama3`, `qwen2.5` | 本地运行，通常无需认证 |
| Custom | 用户指定 | 用户指定 | 兼容 OpenAI Chat Completions 协议 |

> 所有 LLM 提供商必须兼容 OpenAI Chat Completions API 协议，否则需要适配层。

### 2.2 ComfyUI API

#### 2.2.1 版本检测

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | / | 无 | 获取 ComfyUI 版本信息和可用端点 |

**响应**：
```json
{
  "comfyui_version": "0.2.x",
  "api_endpoints": "/api",
  "prompt_endpoint": "/prompt",
  "history_endpoint": "/history"
}
```

#### 2.2.2 提交工作流生成请求

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | /prompt | 无 | 提交工作流生成任务（图片/视频/音频） |

**请求体**（文生图 Txt2Img）：
```json
{
  "workflow_json": {
    "3": {
      "class_type": "CheckpointLoaderSimple",
      "inputs": { "ckpt_name": "model.safetensors" }
    },
    "4": {
      "class_type": "CLIPTextEncode",
      "inputs": { "text": "正面半身，男性角色...", "clip": ["3", 1] }
    },
    ...
  }
}
```

**响应**：
```json
{
  "prompt_id": "unique-id-12345",
  "status": "queued",
  "expected_images": 1
}
```

#### 2.2.3 获取生成历史

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /history/{prompt_id} | 获取指定任务完成结果 |

**响应**：
```json
{
  "prompt_id": "unique-id-12345",
  "outputs": {
    "10": {
      "images": [
        { "filename": "img_0001.png", "type": "output" }
      ]
    }
  }
}
```

#### 2.2.4 WebSocket 实时状态监控

| 协议 | 路径 | 说明 |
|------|------|------|
| WebSocket | /ws?clientId=xxx | 实时接收任务状态 |

**事件消息**：
```json
// 任务开始
{ "type": "executing", "data": { "prompt_id": "unique-id-12345" } }

// 节点执行
{ "type": "executing", "data": { "prompt_id": "unique-id-12345", "node": 10 } }

// 任务完成
{ "type": "executing", "data": { "prompt_id": "unique-id-12345", "node": null } }
```

### 2.3 FFmpeg 集成

#### 2.3.1 版本检查

```bash
ffmpeg -version
```

**成功时包含版本号**：
```
ffmpeg version 6.0...
```

**失败时**：
- 退出码：1
- 标准错误输出：`ffmpeg: command not found`

#### 2.3.2 视频合并

```bash
ffmpeg -f concat -safe 0 -i list.txt -c copy output.mp4
```

**list.txt 格式**：
```
file 'C:/path/to/shot1.mp4'
file 'C:/path/to/shot2.mp4'
...
```

---

## 3. 异常处理

### 3.1 LLM API 调用失败

**降级策略**：
1. 重试 3 次（间隔 1 秒）
2. 仍失败则返回错误信息，用户可手动切换到其他接口重试
3. 错误信息存储于数据库，页面刷新后可看到并操作

### 3.2 ComfyUI 连接失败

**降级策略**：
1. 重试 3 次（间隔 3 秒）
2. 仍失败则返回错误信息，用户可手动操作
3. 状态持久化到数据库，页面刷新后可重新获取

### 3.3 资源生成超时

**策略**：
- 设置超时时间（图片 5 分钟，视频 30 分钟）
- 超时后标记为失败，提示用户重试

### 3.4 FFmpeg 不可用

**策略**：
- 启动时验证 FFmpeg 可用性
- 不可用时显示安装提示，阻止导出操作

---

## 4. 数据同步方案

### 4.1 文本生成（LLM API）同步模式

```
用户操作 → 提交请求 → ApiGateway → 指定 LLM 提供商 API → 返回文本
         → 阻塞等待（最长 X 秒）→ 返回结果 → 存入数据库 → 前端渲染
```

文本调用为同步操作（最多等待 30 秒），流式模式通过 SSE 实时推送增量内容。

### 4.2 素材生成（ComfyUI）异步模式

```
用户操作 → 提交请求 → ComfyUI API → 返回 prompt_id
         → 阻塞等待(最长X分钟) → 轮询历史 → 获取结果
         → 下载文件 → 存入 EntityImage → 返回结果给前端
```

### 4.3 轮询频率

- 图片生成：每 3 秒查询一次
- 视频生成：每 10 秒查询一次
- 音频生成：每 10 秒查询一次

---

## 5. ApiGateway 统一抽象层

### 5.1 架构设计

所有 LLM 调用通过 `IApiGateway` 统一入口，内部路由到具体提供商：

```csharp
public class ApiGateway : IApiGateway
{
    private readonly Dictionary<string, ILLMClient> _providers;

    public async Task<string> ChatAsync(long projectId, string prompt, CancellationToken ct = default)
    {
        var provider = GetProvider(projectId);  // 从 ApiProviders 表获取当前接口的配置
        var client = _providers[provider.ProviderType];
        return await client.ChatAsync(prompt, provider.ApiKey, provider.Model, ct);
    }
}
```

### 5.2 新增 LLM 提供商只需

1. 实现 `ILLMClient` 接口
2. 在 `ApiGateway` 构造函数中注册到字典
3. 前端设置页 `ProviderType` 下拉框添加新枚举值

无需修改调用方业务逻辑。
