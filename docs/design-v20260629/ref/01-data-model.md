# 漫剧开发工具 - 数据模型设计

> **基于需求版本**：docs/req-v20260629/ v2.0 (2026-07-02)
> **关联设计文档**：../design.md

---

## 1. 系统配置层

### 1.1 应用全局配置（appsettings.json，不存入数据库）

```json
{
  "DeepSeek": {
    "ApiKey": "",
    "ApiUrl": "https://api.deepseek.com",
    "Model": "deepseek-chat"
  },
  "Comfyui": {
    "ApiUrl": "http://localhost:8188",
    "WsUrl": "ws://localhost:8188/ws",
    "OutputDir": ""
  },
  "FFmpeg": {
    "ExecutablePath": ""
  }
}
```

---

## 2. 全局审计字段规范

所有实体表必须包含以下审计字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| CreatedTime | long | 创建时间（UTC 毫秒时间戳） |
| UpdatedTime | long | 更新时间（UTC 毫秒时间戳） |

> 本项目为纯本地无用户认证工具，无需 `created_by`、`updated_by`、`is_deleted`、`deleted_time` 字段。

---

## 3. 数据库表结构

### 3.1 项目管理层 — Project

**Projects 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| Name | string(256) | NotNull, Unique | 项目名称 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Project
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public ICollection<Story> Stories { get; set; }
    public ICollection<Episode> Episodes { get; set; }
}
```

---

### 3.2 剧本层

#### 3.2.1 剧本 — Story

**Stories 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| Title | string(512) | NotNull | 剧本标题 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Story
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Title { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<StoryChapter> Chapters { get; set; }
}
```

#### 3.2.2 章节 — StoryChapter

**StoryChapters 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| StoryId | long | FK → Stories | 归属剧本 ID |
| ChapterNumber | string(32) | NotNull | 章节序号（"第一章"、"第二章"...） |
| ChapterName | string(256) | NotNull | 章节名字（如"雨夜决战"） |
| Content | string (max) | NotNull | 章节内容（文本） |
| Order | int | NotNull | 章节排序（从 0 开始） |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class StoryChapter : BaseEntity
{
    public long Id { get; set; }
    public long StoryId { get; set; }
    public string ChapterNumber { get; set; }
    public string ChapterName { get; set; }
    public string Content { get; set; }
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Story Story { get; set; }
}
```

---

### 3.3 资产层 — Assets（统一表）

**Assets 表（替代原 Actor/Prop/Scene/Bgm/Skill 四张表）**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 所属项目 ID |
| ResourceId | long | FK → Resources, Nullable | 资产图片资源 ID |
| AssetType | string(32) | NotNull | 资产类型：Actor/Prop/Scene/Bgm |
| Name | string(256) | NotNull | 资产名称 |
| Description | string (max) | Nullable | 详细描述 |
| ParentId | long | Nullable | 变体引用（同 AssetType 内的父资产 ID，null=不是变体） |
| Order | int | NotNull | 排序（从 0 开始） |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |
| Name | string(256) | NotNull | 资产名称 |
| Description | string (max) | Nullable | 详细描述 |
| ParentId | long | Nullable | 变体引用（同 AssetType 内的父资产 ID，null=不是变体） |
| Order | int | NotNull | 排序（从 0 开始） |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Asset : BaseEntity
{
    public long Id { get; set; }
    public long ProjectId { get; set; }     // 所属项目
    public long? ResourceId { get; set; }   // 资产图片资源 ID
    public string AssetType { get; set; }   // "Actor", "Prop", "Scene", "Bgm"
    public string Name { get; set; }
    public string Description { get; set; }
    public long? ParentId { get; set; }     // 变体引用（nullable，指向同 AssetType 内的父资产）
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public Resource Resource { get; set; }
    public Asset Parent { get; set; }
    public ICollection<Asset> Children { get; set; }
}
```

- `ParentId` 只能指向同 AssetType 内的记录
- Actor 变体 = 子角色（换装）
- Prop 变体 = 真假道具
- Scene 变体 = 季节/环境变体
- Bgm 无变体，但结构一致
- 资产归属项目，通过 ProjectId 关联，同一项目内资产名唯一

---

### 3.4 资源层 — Resource

**Resources 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| MediaType | string(16) | NotNull | 媒体类型：Image/Video/Audio |
| FilePath | string(1024) | NotNull | 文件路径（如 `actor/1/Front.png`） |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Resource : BaseEntity
{
    public long Id { get; set; }
    public string MediaType { get; set; }   // "Image", "Video", "Audio"
    public string FilePath { get; set; }    // 文件路径
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }
}
```

> Resources 表是独立资源库，不绑定任何实体（无 AssetId/ShotId）。文件按路径归类管理，通过路径规则识别归属：`actor/{Id}/Front.png` → Actor Id=1。

---

### 3.5 分集分镜层

#### 3.5.1 分集 — Episode

**Episodes 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| StoryChapterId | long | FK → StoryChapters (Nullable) | 关联剧本章节（可为 null = 手动创建） |
| Name | string(256) | NotNull | 分集名称 |
| Duration | int | NotNull | 时长（秒） |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Episode
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public long? StoryChapterId { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<Shot> Shots { get; set; }
}
```

#### 3.5.2 分镜 — Shot

**Shots 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| EpisodeId | long | FK → Episodes | 集 ID |
| AssetRefs | string (max) | Nullable | 资产引用 JSON（本镜头所用资产） |
| Description | string (max) | NotNull | 镜头描述（完整描述该镜头内容） |
| ShotSize | string(32) | Nullable | 景别（远景/全景/中景/近景/特写） |
| CameraMovement | string(64) | Nullable | 运镜方式（固定/前推/拉远/平移/跟随） |
| Duration | float | Nullable | 镜头时长（秒） |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Shot : BaseEntity
{
    public long Id { get; set; }
    public long EpisodeId { get; set; }
    public string AssetRefs { get; set; }
    public string Description { get; set; }
    public string ShotSize { get; set; }
    public string CameraMovement { get; set; }
    public float? Duration { get; set; }
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Episode Episode { get; set; }
    public ICollection<ShotFrame> Frames { get; set; }
}
```

> AssetRefs 存储该镜头所用资产：`{"actor":[1,2],"scene":[3],"prop":[1]}`。资产引用指向 Assets 表 Id。
> Frames 表管理该镜头的关键帧，AI 生成视频时基于 Frames 描述工作。

#### 3.5.3 分镜帧 — ShotFrame

**ShotFrames 表（归属于 Shot）**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ShotId | long | FK → Shots | 归属分镜 ID |
| ProjectId | long | FK → Projects | 所属项目 ID |
| FrameType | string(32) | NotNull | First / Middle / Last |
| Description | string (max) | NotNull | 帧画面描述 |
| ResourceId | long | FK → Resources, Nullable | 帧图片资源 ID |
| StartTime | float | Nullable | 起始时间（秒，First 帧通常从 0 开始） |
| Duration | float | Nullable | 持续时间（秒） |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class ShotFrame : BaseEntity
{
    public long Id { get; set; }
    public long ShotId { get; set; }
    public long ProjectId { get; set; }
    public string FrameType { get; set; }
    public string Description { get; set; }
    public long? ResourceId { get; set; }
    public float? StartTime { get; set; }
    public float? Duration { get; set; }
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Shot Shot { get; set; }
    public Project Project { get; set; }
    public Resource Resource { get; set; }
}
```

> Shot 创建/自动生成时，AI 根据 `Shot.Description` 产生 N 个帧：
> - Frame #1: Type=First, StartTime=0, Duration=2.0s
> - Frame #2: Type=Middle, StartTime=2.0, Duration=2.5s
> - Frame #3: Type=Middle, StartTime=4.5, Duration=2.0s
> - Frame #4: Type=Last, StartTime=6.5, Duration=3.5s
> 
> ComfyUI 视频生成策略：
> - 用 First Frame 的描述出首帧图
> - 用所有 Frames 的描述 + 时间信息，AI 判断运镜节奏，生成视频
> - 用户可编辑每个帧的 Type/Description/StartTime/Duration

---

### 3.6 提示词模板层 — PromptTemplate

**PromptTemplates 表（数据库存储，非文件）**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| Name | string(256) | NotNull | 模板名称（如"人物档案默认"） |
| TemplateType | string(64) | NotNull | 模板类型 |
| Content | string (max) | NotNull | 模板内容（含占位符如 `{{角色名}}`） |
| IsDefault | bool | NotNull | 是否为默认模板 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**TemplateType 枚举值**：

| 模板类型 | AssetType 对应 | 说明 |
|---------|---------------|------|
| `CharacterProfile` | Actor | 角色档案模板（含四视图绘图布局和格式控制） |
| `PropProfile` | Prop | 道具档案模板（含双视图绘图布局） |
| `SceneProfile` | Scene | 场景档案模板（含场景图绘图描述） |
| `BgmProfile` | Bgm | BGM档案模板（音频生成提示词） |
| `StoryGeneration` | - | 剧本创作系统提示词 |
| `EpisodeBreakdown` | - | 漫剧清单生成系统提示词 |
| `ShotPlanning` | - | 镜头清单生成系统提示词 |

**C# 实体**：
```csharp
public class PromptTemplate : BaseEntity
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string TemplateType { get; set; }
    public string Content { get; set; }
    public bool IsDefault { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }
}
```

> 系统启动时初始化默认模板到 PromptTemplates 表。生成档案时：`WHERE TemplateType = @assetType` 查出模板内容，拼接 `Assets.Description`，调 LLM → 调 ComfyUI 出图。

---

### 3.7 API 接口配置层 — ApiProvider

**ApiProviders 表（单表统一管理所有 API）**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| Name | string(256) | NotNull | API 名称 |
| ApiUrl | string(512) | NotNull | API 地址 |
| ApiKey | string(1024) | Nullable | API Key |
| ConfigJson | string (max) | Nullable | 额外配置 |
| IsDefault | bool | NotNull | 是否默认 |
| IsActive | bool | NotNull | 是否启用 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class ApiProvider : BaseEntity
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string ApiUrl { get; set; }
    public string ApiKey { get; set; }
    public string ConfigJson { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }
}
```

> ConfigJson 内容依 API 类型而定：
> - LLM API（如 DeepSeek）：`{"model":"deepseek-chat"}`
> - ComfyUI 角色图生成 API：`{"workflow_type":"ActorFourView","prompt_key":"positive_prompt"}`
> - ComfyUI 视频生成 API：`{"workflow_type":"Img2Video"}`

**典型 API 配置示例**：

```json
// LLM 文本生成（统一一个就够了）
{ "Name": "DeepSeek主接口", "ApiUrl": "https://api.deepseek.com", "ApiKey": "sk-xxx", "ConfigJson": "{\"model\":\"deepseek-chat\"}" }

// ComfyUI 各类生成 API（根据需要添加多条）
{ "Name": "角色四视图生成", "ApiUrl": "http://localhost:8188", "ConfigJson": "{\"workflow_type\":\"ActorFourView\"}" }
{ "Name": "场景主图生成", "ApiUrl": "http://localhost:8188", "ConfigJson": "{\"workflow_type\":\"SceneImage\"}" }
{ "Name": "图片转视频", "ApiUrl": "http://localhost:8188", "ConfigJson": "{\"workflow_type\":\"Img2Video\"}" }
```

---

## 4. 数据流说明

### 4.1 完整数据流

```
[Step 1] 创建项目
  Projects 表

[Step 2] 剧本创作（只生成文本）
  Story.Title + StoryChapters.Content  ← 来自 AI 对话或手动输入

[Step 3] 全局资产生成（一次性，从全部章节提取）
  Assets 表                          ← AI 扫描所有章节文本，提取人物/道具/场景/BGM
  用户审核 → 确保同一角色描述全局一致

[Step 4] 资产图片生成（按资产逐个）
  Resources 表                       ← Assets → LLM 提示词 → ComfyUI 出图 → 存入 Resources

[Step 5] 分集+分镜生成（按章节）
  Episode 表 + StoryChapterId         ← 用户选章节触发，AI 生成分集
  Shots 表 + AssetRefs                ← 关联 Assets.Id，通过 JSON 引用资产
  ShotFrames 表 (First/Middle/Last + Desc + StartTime + Duration) ← AI 生成关键帧时间线
  用户审核 → 调整分镜/挑选资产/编辑帧

[Step 6] 分镜资源生成（按分镜逐个）
  Resources 表                       ← Shot.Frames[First] → ComfyUI 出首帧图
                                       ← 所有 Frames + 时间线 → ComfyUI 出视频

[Step 7] 视频导出
  FFmpeg 合并所有 Shots 的 Resources.Videos → 导出成品
```

一句话：**先文字（剧本），再资产，再资产图片，再分集分镜，再分镜资源，最后导出。**

### 4.2 数据流向图

```
ShotContent ──→ Assets ──→ Resources ──→ Episode ──→ Shots ──→ ShotFrames ──→ Resources ──→ FFmpeg
                  ↑            ↑               ↑            ↑              ↑
             资产表全局       资产图片资源    分集列表     分镜列表      首帧+视频资源
            唯一不重复       (Actor图等)    (可选章节)   (按章节生成)  (Shot图等)
```

**关键约束：**
- Asset 全局唯一，不绑定项目
- Resources 通过 FilePath 路径识别归属（`asset_type/id/view_type.ext`）
- Shot 通过 AssetRefs JSON 引用 Assets，软关联
- Episode 通过 StoryChapterId 关联对应章节

---

## 5. 实体关系图

```mermaid
erDiagram
    Project ||--o{ Story
    Project ||--o{ Episode
    Project ||--o{ Workflow
    Story ||--o{ Chapter
    Episode ||--o{ Shot
    Shot ||--o{ ShotFram
    ShotFram ||--o| Resource
    Asset ||--o| Resource
    Asset ||--o{ Asset

    Project {
        int Id
        varchar Name
    }
    Story {
        int Id
        int ProjectId
        varchar Title
    }
    Chapter {
        int Id
        int StoryId
        varchar Number
        varchar Name
        text Content
        int Order
    }
    Episode {
        int Id
        int ProjectId
        int ChapterId
        varchar Name
        int Duration
        int Order
    }
    Shot {
        int Id
        int EpisodeId
        text AssetRefs
        text Description
        varchar ShotSize
        varchar CameraMovement
        float Duration
        int Order
    }
    ShotFram {
        int Id
        int ShotId
        int ProjectId
        varchar FrameType
        text Description
        int ResourceId
        float StartTime
        float Duration
        int Order
    }
    Asset {
        int Id
        int ProjectId
        int ResourceId
        varchar Type
        varchar Name
        text Description
        int ParentId
        int Order
    }
    Resource {
        int Id
        varchar MediaType
        varchar FilePath
    }
    Workflow {
        int Id
        int ProjectId
        varchar Name
        varchar Type
        text Config
    }
    PromptTpl {
        int Id
        varchar Name
        varchar Type
        text Content
        bool Default
    }
    ApiProvider {
        int Id
        varchar Name
        varchar Url
        varchar Key
        text Config
        bool Default
        bool Active
    }
```

**资产复用（跨项目）：**
```
Assets 表（全局通用）：
  Id=1, AssetType=Actor, Name="英雄A", Description="男，20岁，黑短发"
  Id=2, AssetType=Actor, Name="英雄A(校服)", ParentId=1, Description="校服版"

项目A 的 Shot：
  AssetRefs = {"actor":[1],"scene":[1],"prop":[2]}

项目B 的 Shot：
  AssetRefs = {"actor":[1],"scene":[3],"bgm":[2]}
  ← Assets.Id=1 可在多个项目中使用，无需重复创建
```

---

## 6. 存储路径规则

全局统一目录（Assets 跨项目通用）：

```
wwwroot/asset/
├── actor/
│   └── {AssetId}/
│       ├── Front.png
│       ├── Back.png
│       ├── Side.png
│       └── ThreeQuarter.png
├── prop/
│   └── {AssetId}/
│       ├── Front.png
│       └── Side.png
├── scene/
│   └── {AssetId}/
│       └── Main.png
├── bgm/
│   └── {AssetId}/
│       └── Audio.mp3
└── shot/
    └── {ShotId}/
        ├── FirstFrame.png
        ├── Video.mp4
        └── frames/
            ├── 0.png              ← Frame#0 (First 帧图片)
            ├── 1.png              ← Frame#1 (Middle 帧图片)
            └── 2.png              ← Frame#2 (Last 帧图片)
```

`Resources.FilePath` 存储相对路径，如 `actor/1/Front.png`、`shot/3/Video.mp4`。

---

## 7. 数据库表汇总

| 表名 | 说明 | 主键 | 外键 | 审计字段 |
|------|------|------|------|---------|
| Projects | 项目 | Id | - | CreatedTime, UpdatedTime |
| Stories | 剧本 | Id | ProjectId | CreatedTime, UpdatedTime |
| StoryChapters | 章节 | Id | StoryId | CreatedTime, UpdatedTime |
| Assets | 资产（统一） | Id | ParentId | CreatedTime, UpdatedTime |
| Resources | 资源文件 | Id | - | CreatedTime, UpdatedTime |
| Episodes | 分集 | Id | ProjectId, StoryChapterId | CreatedTime, UpdatedTime |
| Shots | 分镜 | Id | EpisodeId | CreatedTime, UpdatedTime |
| ShotFrames | 分镜关键帧 | Id | ShotId | CreatedTime, UpdatedTime |
| PromptTemplates | 提示词模板 | Id | - | CreatedTime, UpdatedTime |
| ApiProviders | API接口配置 | Id | - | CreatedTime, UpdatedTime |
