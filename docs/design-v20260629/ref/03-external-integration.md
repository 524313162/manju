# 漫剧开发工具 - 外部系统集成接口

> **基于需求版本**：docs/req-v20260629/
> **关联设计文档**：[design.md](design.md)

---

## 1. 外部系统清单

| 系统名称 | 技术栈 | 集成方式 | 用途 |
|----------|--------|----------|------|
| ComfyUI | Python/WebSocket | HTTP API + WebSocket | AI 素材生成（图片/视频/音乐/提示词） |
| ffmpeg | CLI | 进程调用 | 视频合并导出 |
| 本地磁盘 | 文件系统 | 文件读写 | 素材存储、图片/视频替换 |

---

## 2. 接口规范

### 2.1 ComfyUI API

#### 2.1.1 版本检测

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/v1 | 无 | 获取 ComfyUI 版本信息和可用端点 |

**响应**：
```json
{
  "comfyui_version": "0.2.x",
  "api_endpoints": "/api/v1",
  "prompt_endpoint": "/prompt",
  "history_endpoint": "/history"
}
```

#### 2.1.2 提交提示词生成请求

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | /prompt | 无 | 提交工作流生成任务 |

**请求体**（以 TextGen 为例）：
```json
{
  "workflow": {
    "prompt": "输入文本",
    "system": "提示词生成系统提示词"
  }
}
```

**响应**：
```json
{
  "prompt_id": "unique-id-12345",
  "status": "queued"
}
```

#### 2.1.3 提交图片生成请求

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /prompt | 提交文生图工作流 |

**请求体**（Txt2Img 文生图）：
```json
{
  "workflow": {
    "prompt": "正面半身，...|背面半身，...|侧面半身，...|三视角半...",
    "negative_prompt": "负面提示词",
    "width": 512,
    "height": 512,
    "seed": 12345,
    "cfg_scale": 7.5,
    "steps": 20,
    "batch_count": 1
  }
}
```

**响应**：
```json
{
  "prompt_id": "unique-id-12345",
  "status": "queued",
  "expected_images": 4  // Quads, double, or single
}
```

#### 2.1.4 提交视频生成请求

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /prompt | 提交图生视频工作流 |

**请求体**（Img2Video 图生视频）：
```json
{
  "workflow": {
    "image_file": "/绝对路径/FirstFrame.png",
    "prompt": "镜头移动描述",
    "frames": 24,
    "fps": 24,
    "seed": 12345,
    "cfg_scale": 7.5,
    "steps": 20
  }
}
```

#### 2.1.5 获取生成历史

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /history/{prompt_id} | 获取指定任务完成结果 |

**响应**：
```json
{
  "prompt_id": "unique-id-12345",
  "status": "completed",
  "outputs": {
    "images": [
      { "filename": "img_0001.png", "type": "output" },
      { "filename": "img_0002.png", "type": "output" }
    ],
    "video": [
      { "filename": "video_0001.mp4", "type": "output" }
    ]
  }
}
```

#### 2.1.6 WebSocket 实时状态监控

| 协议 | 路径 | 说明 |
|------|------|------|
| WebSocket | /ws/events?clientId=xxx | 实时接收任务状态 |

**事件消息**：
```json
// 任务开始
{ "type": "executing", "data": { "prompt_id": "unique-id-12345" } }

// 节点执行中
{
  "type": "executing",
  "data": {
    "prompt_id": "unique-id-12345",
    "node": null
  }
}

// 任务完成
{
  "type": "executing",
  "data": {
    "prompt_id": "unique-id-12345",
    "node": null,
    "outputs": { /* 同上 */ }
  }
}
```

---

### 2.2 FFmpeg 集成

#### 2.2.1 版本检查

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

#### 2.2.2 视频合并

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

### 3.1 ComfyUI 连接失败

**降级策略**：
1. 重试 3 次（间隔 3 秒）
2. 仍失败则返回错误信息，用户可手动操作
3. 状态持久化到数据库，页面刷新后可重新获取

### 3.2 资源生成超时

**策略**：
- 设置超时时间（图片 5 分钟，视频 30 分钟）
- 超时后标记为失败，提示用户重试

### 3.3 FFmpeg 不可用

**策略**：
- 启动时验证 FFmpeg 可用性
- 不可用时显示安装提示，阻止导出操作

---

## 4. 数据同步方案

### 4.1 异步任务模式

```
Blazor 页面 → 提交请求 → ComfyUI API → 返回 prompt_id
          → 阻塞等待(最长X分钟) → 轮询历史 → 获取结果
          → 下载文件 → 存入 EntityImage → 返回结果给前端
```

### 4.2 轮询频率

- 图片生成：每 3 秒查询一次
- 视频生成：每 10 秒查询一次
- 音频生成：每 10 秒查询一次
