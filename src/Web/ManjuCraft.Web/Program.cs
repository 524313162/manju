using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.AI;

using ManjuCraft.Domain.Models;
using ManjuCraft.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/manjucraft-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSingleton(Log.Logger);

var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "manju.db");
builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IProjectDbContext>(sp => sp.GetRequiredService<ProjectDbContext>());

builder.Services.AddScoped<IFileStorageService, FileStorageService>();
//builder.Services.AddScoped<IComfyuiClient, ComfyuiClient>();
//builder.Services.AddScoped<ComfyuiWebSocketListener>();
//builder.Services.AddScoped<ComfyuiTaskPoller>();
//builder.Services.AddScoped<IComfyuiConnectionService, ComfyuiConnectionService>();

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IShotService, ShotService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IShotFrameService, ShotFrameService>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("ComfyuiProxy");

builder.Services.AddScoped<ComfyuiProxyApi>();
builder.Services.AddScoped<IAiClientRegistry, AiClientRegistry>();

builder.Services.AddScoped<IAiTextService, AiTextService>();
builder.Services.AddScoped<IAiMediaService, AiMediaService>();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
    SeedTemplates(db);
    SeedApiProviders(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "api",
    pattern: "api/v1/{controller}/{action}/{id?}");

app.Run();

static void SeedTemplates(ProjectDbContext db)
{
    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    var templates = new[]
    {
        ("系统提示词 - AI剧本创作", "SystemPrompt", @"你是一个专业的剧本创作AI。请按照严格的JSON格式返回剧本的所有内容。

你的输出必须是一个JSON对象，包含以下10个字段：
1. name - 剧本名称（字符串）
2. content - 剧本内容概述/故事梗概（字符串）
3. chapters - 章节数组，每个章节包含 chapterNumber（章节编号字符串）、chapterName（章节名称）、content（章节详细内容）
4. characters - 角色清单数组，每个角色包含 name（角色名称）、description（角色描述）
5. scenes - 场景清单数组，每个场景包含 name（场景名称）、description（场景描述）
6. bgms - BGM清单数组，每个BGM包含 name（BGM名称）、description（音乐风格/用途描述）
7. props - 道具清单数组，每个道具包含 name（道具名称）、description（道具描述/用途）
8. skills - 技能清单数组，每个技能包含 name（技能名称）、description（技能描述/效果说明）

必须严格按照以下JSON结构输出，使用代码块包裹：
```json
{
  ""name"": ""剧本名称"",
  ""content"": ""故事梗概或剧本整体概述"",
  ""chapters"": [
    {
      ""chapterNumber"": ""第一章"",
      ""chapterName"": ""章节名称"",
      ""content"": ""章节详细内容，包含场景描写和对话""
    }
  ],
  ""characters"": [
    {
      ""name"": ""角色名称"",
      ""description"": ""角色的详细描述，包括外貌、性格、背景等""
    }
  ],
  ""scenes"": [
    {
      ""name"": ""场景名称"",
      ""description"": ""场景的详细描述，包括环境、氛围、视觉元素等""
    }
  ],
  ""bgms"": [
    {
      ""name"": ""BGM名称"",
      ""description"": ""BGM的描述，包括音乐风格、情绪、使用场景""
    }
  ],
  ""props"": [
    {
      ""name"": ""道具名称"",
      ""description"": ""道具的描述，包括外观、功能、象征意义""
    }
  ],
  ""skills"": [
    {
      ""name"": ""技能名称"",
      ""description"": ""技能的描述，包括效果、使用方式、限制""
    }
  ]
}

每个字段都必须存在且不能为空。chapters至少包含3章。每个清单项都必须有说明。"
),
        ("剧本创作提示词 - 系统", "StoryGeneration", "你是一个专业的剧本创作AI，擅长将故事转化为完整的剧本。请根据用户的标题和故事主题，生成完整的剧本内容。注意：你的输出必须是严格的JSON格式，包含10个字段：name（剧本名称）、content（故事梗概）、chapters（章节数组，每章含chapterNumber/chapterName/content）、characters（角色清单含name/description）、scenes（场景清单含name/description）、bgms（音乐清单含name/description）、props（道具清单含name/description）、skills（技能清单含name/description）。每个清单项都必须有说明。输出时请使用代码块包裹JSON。"
),
        ("漫剧清单默认", "EpisodeBreakdown", "你是一个剧本分析专家。从给定的故事内容中提取所有角色、道具、场景和BGM。每个资产需要名称和详细描述。以JSON格式返回。"
),
        ("镜头清单默认", "ShotPlanning", "你是一个视频导演。根据动态描述生成视频生成提示词，包括运镜、氛围、画质要求。以JSON格式返回。"
),
        ("人物档案默认", "CharacterProfile", "你是一个角色设计师。根据角色描述，生成完整的角色档案，包括外貌特征、性格特点、背景故事和英文生图提示词。以JSON格式返回。"
),
        ("场景档案默认", "SceneProfile", "你是一个场景设计师。根据场景描述，生成详细的场景档案和英文生图提示词。以JSON格式返回。"
),
        ("道具档案默认", "PropProfile", "你是一个道具设计师。根据道具描述，生成详细的道具档案和英文生图提示词。以JSON格式返回。"
),
        ("BGM档案默认", "BgmProfile", "你是一个音乐制作人。根据BGM描述，生成详细的音乐风格说明和英文音频生成提示词。以JSON格式返回。"
),
        ("技能档案默认", "SkillProfile", "你是一个游戏/动画设计师。根据技能描述，生成完整的技能档案，包括技能名称、效果描述、使用方式、视觉表现和英文提示词。以JSON格式返回。"
)
    };

    foreach (var (name, type, content) in templates)
    {
        if (!db.PromptTemplates.Any(t => t.TemplateType == type))
        {
            db.PromptTemplates.Add(new PromptTemplate { Name = name, TemplateType = type, IsDefault = true, Content = content, CreatedTime = now, UpdatedTime = now });
        }
    }
    db.SaveChanges();
}

static void SeedApiProviders(ProjectDbContext db)
{
    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    if (!db.ApiProviders.Any(p => p.Name == "DeepSeek"))
    {
        db.ApiProviders.Add(new ApiProvider
        {
            Name = "DeepSeek",
            Capability = AiCapability.TextToText,
            ApiUrl = "https://api.deepseek.com/v1",
            ApiKey = "",
            Model = "deepseek-v4-flash",
            CreatedTime = now,
            UpdatedTime = now
        });
    }

    if (!db.ApiProviders.Any(p => p.Name == "ComfyUI"))
    {
        db.ApiProviders.Add(new ApiProvider
        {
            Name = "ComfyUI",
            Capability = AiCapability.TextToVideo,
            ApiUrl = "http://localhost:8188",
            ApiKey = "",
            Model = "LLM-QWEN.json",
            CreatedTime = now,
            UpdatedTime = now
        });
    }

    db.SaveChanges();
}
