using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;
using ManjuCraft.Infrastructure.Service;
using ManjuCraft.Application.Service;
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
builder.Services.AddScoped<IComfyuiClient, ComfyuiClient>();
builder.Services.AddScoped<ComfyuiWebSocketListener>();
builder.Services.AddScoped<ComfyuiTaskPoller>();
builder.Services.AddScoped<IComfyuiConnectionService, ComfyuiConnectionService>();
builder.Services.AddScoped<IFfmpegService, FfmpegService>();

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IShotService, ShotService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();

builder.Services.AddHttpClient();
builder.Services.Configure<DeepSeekOptions>(builder.Configuration.GetSection(DeepSeekOptions.SectionName));
builder.Services.AddScoped<IDeepSeekService, DeepSeekService>();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    dbContext.Database.EnsureCreated();
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

app.Run();
