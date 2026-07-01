# 漫剧开发工具 - 数据模型设计

> **基于需求版本**：docs/req-v20260629/
> **关联设计文档**：../design.md

---

## 1. 系统配置层

### 1.1 应用全局配置（appsettings.json，不存入数据库）

```json
{
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

配置加载策略：
1. 优先读取项目级 `ComfyuiConfigJson`（如存在则合并覆盖）
2. 无项目级配置时使用全局默认 `appsettings.json`

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
| ComfyuiConfigJson | string | Nullable | 项目级 ComfyUI 配置 JSON，null=使用全局默认 |
| CreatedTime | long | NotNull | 创建时间（UTC 毫秒时间戳） |
| UpdatedTime | long | NotNull | 更新时间（UTC 毫秒时间戳） |

**C# 实体**：
```csharp
public class Project
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string ComfyuiConfigJson { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public ICollection<Story> Stories { get; set; }
    public ICollection<Actor> Actors { get; set; }
    public ICollection<Prop> Props { get; set; }
    public ICollection<Scene> Scenes { get; set; }
    public ICollection<Skill> Skills { get; set; }
    public ICollection<Bgm> Bgms { get; set; }
    public ICollection<Episode> Episodes { get; set; }
    public ICollection<EntityImage> EntityImages { get; set; }
    public ICollection<Workflow> Workflows { get; set; }
}
```

---

### 3.2 故事层 — Story

**Stories 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| Content | string (max) | NotNull | 故事内容 |
| SplitContent | string (max) | Nullable | 拆分后的内容 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Story
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Content { get; set; }
    public string SplitContent { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<Actor> Actors { get; set; }
}
```

---

### 3.3 资产层

#### 3.3.1 演员 — Actor

**Actors 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| Name | string(256) | NotNull | 演员名称 |
| Description | string (max) | Nullable | 详细描述 |
| FourViewPrompt | string (max) | Nullable | 四视图生成提示词 |
| DefaultWorkflowType | string(50) | NotNull | 默认工作流类型：Txt2Img |
| Order | int | NotNull | 排序（从 0 开始） |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Actor
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string FourViewPrompt { get; set; }
    public string DefaultWorkflowType { get; set; }  // "Txt2Img"
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<EntityImage> Images { get; set; }
}
```

#### 3.3.2 道具 — Prop

**Props 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| Name | string(256) | NotNull | 道具名称 |
| TwoViewPrompt | string (max) | Nullable | 双视图生成提示词 |
| DefaultWorkflowType | string(50) | NotNull | 默认工作流类型：Txt2Img |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Prop
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; }
    public string TwoViewPrompt { get; set; }
    public string DefaultWorkflowType { get; set; }  // "Txt2Img"
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<EntityImage> Images { get; set; }
}
```

#### 3.3.3 场景 — Scene

**Scenes 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| Name | string(256) | NotNull | 场景名称 |
| ImagePrompt | string (max) | Nullable | 场景图生成提示词 |
| DefaultWorkflowType | string(50) | NotNull | 默认工作流类型：Txt2Img |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Scene
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; }
    public string ImagePrompt { get; set; }
    public string DefaultWorkflowType { get; set; }  // "Txt2Img"
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<EntityImage> Images { get; set; }
}
```

#### 3.3.4 技能 — Skill

**Skills 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| Name | string(256) | NotNull | 技能名称 |
| Prompt | string (max) | Nullable | 技能图生成提示词 |
| DefaultWorkflowType | string(50) | NotNull | 默认工作流类型：Txt2Img |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Skill
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; }
    public string Prompt { get; set; }
    public string DefaultWorkflowType { get; set; }  // "Txt2Img"
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<EntityImage> Images { get; set; }
}
```

#### 3.3.5 背景音乐 — Bgm

**Bgms 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| Name | string(256) | NotNull | BGM 名称 |
| Prompt | string (max) | Nullable | BGM 生成提示词 |
| DefaultWorkflowType | string(50) | NotNull | 默认工作流类型：MusicGen |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Bgm
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; }
    public string Prompt { get; set; }
    public string DefaultWorkflowType { get; set; }  // "MusicGen"
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
}
```

---

### 3.4 分集分镜层

#### 3.4.1 分集 — Episode

**Episodes 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
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
    public string Name { get; set; }
    public int Duration { get; set; }
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
    public ICollection<Shot> Shots { get; set; }
}
```

#### 3.4.2 分镜 — Shot

**Shots 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| EpisodeId | long | FK → Episodes | 集 ID |
| FirstFramePrompt | string (max) | NotNull | 首帧图生成提示词 |
| FirstFrameWorkflowType | string(50) | NotNull | 首帧默认工作流类型：Img2Img 或 Txt2Img |
| Dialog | string (max) | Nullable | 台词描述 |
| VideoPrompt | string (max) | NotNull | 镜头视频生成提示词 |
| VideoWorkflowType | string(50) | NotNull | 视频默认工作流类型：Img2Video 或 Txt2Video |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Shot
{
    public long Id { get; set; }
    public long EpisodeId { get; set; }
    public string FirstFramePrompt { get; set; }
    public string FirstFrameWorkflowType { get; set; }  // "Img2Img" 或 "Txt2Img"
    public string Dialog { get; set; }
    public string VideoPrompt { get; set; }
    public string VideoWorkflowType { get; set; }  // "Img2Video" 或 "Txt2Video"
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Episode Episode { get; set; }
    public ICollection<EntityImage> Images { get; set; }
}
```

---

### 3.5 实体资源层 — EntityImage

**EntityImages 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects | 项目 ID |
| EntityType | string(50) | NotNull | 实体类型：Actor/Prop/Scene/Skill/Shot |
| EntityId | long | NotNull | 关联实体 ID |
| ViewType | string(50) | NotNull | 视图类型 |
| MediaType | string(10) | NotNull | 媒体类型：Image/Video/Audio |
| FilePath | string(1024) | NotNull | 文件路径 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class EntityImage
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string EntityType { get; set; }  // "Actor", "Prop", "Scene", "Skill", "Shot"
    public long EntityId { get; set; }
    public string ViewType { get; set; }   // 视图类型（见下方枚举值）
    public string MediaType { get; set; }  // "Image", "Video", "Audio"
    public string FilePath { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
}
```

**ViewType 枚举值**：

| 实体类型 | ViewType 值 | 说明 |
|---------|-------------|------|
| Actor | Front | 正面四视图 |
| Actor | Back | 背面四视图 |
| Actor | Side | 侧面四视图 |
| Actor | ThreeQuarter | 三视角四视图 |
| Prop | Front | 正面双视图 |
| Prop | Side | 侧面双视图 |
| Scene | Main | 场景主图 |
| Skill | Main | 技能图 |
| Shot | FirstFrame | 首帧图 |
| Shot | Video | 镜头视频 |
| Bgm | Audio | BGM 音频 |

---

### 3.6 工作流配置层 — Workflow

**Workflows 表**

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | long | PK, AutoIncrement | 主键 |
| ProjectId | long | FK → Projects, Nullable | 项目 ID（null=全局默认） |
| Name | string(256) | NotNull | 工作流名称 |
| WorkflowType | string(50) | NotNull | 工作流类型 |
| ConfigJson | string (max) | NotNull | 工作流 JSON 配置 |
| Order | int | NotNull | 排序 |
| CreatedTime | long | NotNull | 创建时间 |
| UpdatedTime | long | NotNull | 更新时间 |

**C# 实体**：
```csharp
public class Workflow
{
    public long Id { get; set; }
    public long? ProjectId { get; set; }
    public string Name { get; set; }
    public string WorkflowType { get; set; }
    public string ConfigJson { get; set; }
    public int Order { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }

    public Project Project { get; set; }
}
```

---

## 4. 工作流类型清单

| 工作流类型 | 英文标识 | 用途 | 默认绑定 |
|-----------|---------|------|---------|
| 文字生成 | TextGen | 自动生成提示词 | Actor/Prop/Scene/Skill/Bgm |
| 文生图 | Txt2Img | 生成角色/道具/场景/技能图片 | Actor/Prop/Scene/Skill |
| 图生图 | Img2Img | 增强/调整首帧图 | Shot.FirstFrame |
| 图生视频 | Img2Video | 首帧图+提示词→视频 | Shot.Video |
| 文生视频 | Txt2Video | 提示词→视频（可选） | Shot.Video |
| 音乐生成 | MusicGen | 提示词→BGM 音频 | Bgm |
| 故事拆分 | StorySplit | 故事→分镜片段 | Story |

---

## 5. 实体关系图

```mermaid
erDiagram
    Project ||--o{ Story : has
    Project ||--o{ Actor : has
    Project ||--o{ Prop : has
    Project ||--o{ Scene : has
    Project ||--o{ Skill : has
    Project ||--o{ Bgm : has
    Project ||--o{ Episode : has
    Project ||--o{ EntityImage : has
    Project ||--o| Workflow : optional (null=全局)
    Episode ||--o{ Shot : has
    Story ||--o{ Actor : "故事生成角色"

    EntityImage }o--|| Actor : "四视图"
    EntityImage }o--|| Prop : "双视图"
    EntityImage }o--|| Scene : "场景图"
    EntityImage }o--|| Skill : "技能图"
    EntityImage }o--|| Shot : "首帧图+视频"
    EntityImage }o--|| Bgm : "音频"
```

---

## 6. 存储路径规则

```
wwwroot/{ProjectId}/asset/
├── actor/
│   └── {ActorId}/
│       ├── Front.png        (四视图: Front)
│       ├── Back.png         (四视图: Back)
│       ├── Side.png         (四视图: Side)
│       └── ThreeQuarter.png (四视图: ThreeQuarter)
├── prop/
│   └── {PropId}/
│       ├── Front.png         (双视图: Front)
│       └── Side.png          (双视图: Side)
├── scene/
│   └── {SceneId}/
│       └── Main.png          (场景主图)
├── skill/
│   └── {SkillId}/
│       └── Main.png          (技能图)
├── shot/
│   └── {ShotId}/
│       ├── FirstFrame.png    (首帧图)
│       └── Video.mp4         (镜头视频)
└── bgm/
    └── {BgmId}/
        └── Audio.mp3         (BGM 音频)
```

**路径模板**：`wwwroot/{ProjectId}/asset/{EntityType}/{EntityId}/{ViewType}.{ext}`

---

## 7. 数据库表汇总

| 表名 | 说明 | 主键 | 外键 | 审计字段 |
|------|------|------|------|---------|
| Projects | 项目 | Id | - | CreatedTime, UpdatedTime |
| Stories | 故事 | Id | ProjectId | CreatedTime, UpdatedTime |
| Actors | 演员 | Id | ProjectId | CreatedTime, UpdatedTime |
| Props | 道具 | Id | ProjectId | CreatedTime, UpdatedTime |
| Scenes | 场景 | Id | ProjectId | CreatedTime, UpdatedTime |
| Skills | 技能 | Id | ProjectId | CreatedTime, UpdatedTime |
| Bgms | BGM | Id | ProjectId | CreatedTime, UpdatedTime |
| Episodes | 分集 | Id | ProjectId | CreatedTime, UpdatedTime |
| Shots | 分镜 | Id | EpisodeId | CreatedTime, UpdatedTime |
| EntityImages | 实体资源 | Id | ProjectId | CreatedTime, UpdatedTime |
| Workflows | 工作流配置 | Id | ProjectId (nullable) | CreatedTime, UpdatedTime |

---

## 8. EF Core 配置要点

```csharp
public class ProjectDbContext : DbContext
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Prop> Props => Set<Prop>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Bgm> Bgms => Set<Bgm>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Shot> Shots => Set<Shot>();
    public DbSet<EntityImage> EntityImages => Set<EntityImage>();
    public DbSet<Workflow> Workflows => Set<Workflow>();

    // SQLite 连接字符串
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=manju.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 一对多关系
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Stories)
            .WithOne(s => s.Project)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Project 外键关系...（同理 Actor, Prop, Scene, Skill, Bgm, Episode, Workflow）
        
        // Episode → Shot
        modelBuilder.Entity<Episode>()
            .HasMany(e => e.Shots)
            .WithOne(s => s.Episode)
            .HasForeignKey(s => s.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```
