using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;
using ManjuCraft.Infrastructure.Service;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.LLM;
using ManjuCraft.Application.LLM.Providers;
using ManjuCraft.Web.LLM;
using ManjuCraft.Domain.Models;
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
builder.Services.AddScoped<IComfyuiClient, ComfyuiClient>();
builder.Services.AddScoped<ComfyuiWebSocketListener>();
builder.Services.AddScoped<ComfyuiTaskPoller>();
builder.Services.AddScoped<IComfyuiConnectionService, ComfyuiConnectionService>();

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IShotService, ShotService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IShotFrameService, ShotFrameService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();

builder.Services.AddHttpClient();
builder.Services.Configure<DeepSeekOptions>(builder.Configuration.GetSection(DeepSeekOptions.SectionName));
builder.Services.AddScoped<IDeepSeekService, DeepSeekService>();

builder.Services.AddScoped<ILLMClient, MockLLMClient>();
// 生产环境切换: builder.Services.AddScoped<ILLMClient, DeepSeekClient>();
// 或其他:        builder.Services.AddScoped<ILLMClient, NvidiaClient>();
//                builder.Services.AddScoped<ILLMClient, OllamaClient>();

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
    if (db.PromptTemplates.Any()) return;

    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    db.PromptTemplates.AddRange(
        new PromptTemplate { Name = "剧本创作默认", TemplateType = "StoryGeneration", IsDefault = true,
            Content = "你是一个专业的剧本创作助手。根据用户提供的标题和故事主题，生成包含3-4章的完整剧本大纲。每章需要包含：章节编号、章节名称、详细的场景描写和对话内容。请以JSON格式返回。", CreatedTime = now, UpdatedTime = now },
        new PromptTemplate { Name = "漫剧清单默认", TemplateType = "EpisodeBreakdown", IsDefault = true,
            Content = "你是一个剧本分析专家。从给定的故事内容中提取所有角色、道具、场景和BGM。每个资产需要名称和详细描述。以JSON格式返回。", CreatedTime = now, UpdatedTime = now },
        new PromptTemplate { Name = "镜头清单默认", TemplateType = "ShotPlanning", IsDefault = true,
            Content = "你是一个视频导演。根据动态描述生成视频生成提示词，包括运镜、氛围、画质要求。以JSON格式返回。", CreatedTime = now, UpdatedTime = now },
        new PromptTemplate { Name = "人物档案默认", TemplateType = "CharacterProfile", IsDefault = true,
            Content = "你是一个角色设计师。根据角色描述，生成完整的角色档案，包括外貌特征、性格特点、背景故事和英文生图提示词。以JSON格式返回。", CreatedTime = now, UpdatedTime = now },
        new PromptTemplate { Name = "场景档案默认", TemplateType = "SceneProfile", IsDefault = true,
            Content = "你是一个场景设计师。根据场景描述，生成详细的场景档案和英文生图提示词。以JSON格式返回。", CreatedTime = now, UpdatedTime = now },
        new PromptTemplate { Name = "道具档案默认", TemplateType = "PropProfile", IsDefault = true,
            Content = "你是一个道具设计师。根据道具描述，生成详细的道具档案和英文生图提示词。以JSON格式返回。", CreatedTime = now, UpdatedTime = now },
        new PromptTemplate { Name = "BGM档案默认", TemplateType = "BgmProfile", IsDefault = true,
            Content = "你是一个音乐制作人。根据BGM描述，生成详细的音乐风格说明和英文音频生成提示词。以JSON格式返回。", CreatedTime = now, UpdatedTime = now }
    );
    db.SaveChanges();
}