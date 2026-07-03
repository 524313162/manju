using ComfyuiProxy.Web.Services;
using ManjuCraft.Domain.Models;
using ManjuCraft.Domain.Models.ComfyUI;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
.WriteTo.File("logs/proxy-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Warning)
.CreateLogger();

builder.Host.UseSerilog(Log.Logger);

// ComfyUI 连接配置
builder.Services.Configure<ComfyuiOptions>(options =>
{
    options.BaseUrl = builder.Configuration.GetValue<string>("ComfyUI:BaseUrl") ?? "http://localhost:8188";
});

// 依赖注入
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddScoped<ComfyuiProxyService>();
builder.Services.AddSingleton<TaskManager>();

// Swagger 配置
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ComfyUI代理API", Version = "v1" });
    try
    {
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "ComfyuiProxy.Web.xml"), true);
    }
    catch { }
});



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ComfyUI代理API v1");
});

// 静态文件服务 — 输出文件
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
    Path.Combine(AppContext.BaseDirectory, "output")),
    RequestPath = "/output"
});

app.MapControllers();

app.Run();
