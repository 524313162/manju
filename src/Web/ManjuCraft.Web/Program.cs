using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.AI;
using ManjuCraft.Infrastructure;
using ManjuCraft.Web.Services;
using Serilog;

namespace ManjuCraft.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
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
            db.Database.EnsureCreated();
            await DatabaseSeeder.SeedAsync(db);
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
    }
}
