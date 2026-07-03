using ComfyuiProxy.Web.Services;
using ManjuCraft.Domain.Models;
using ManjuCraft.Domain.Models.ComfyUI;
using ManjuCraft.Infrastructure.Service;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/proxy-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Warning)
    .CreateLogger();

builder.Services.AddSingleton(Log.Logger);

// ComfyUI 连接配置
builder.Services.Configure<ComfyuiOptions>(options =>
{
    options.BaseUrl = builder.Configuration["ComfyUI:BaseUrl"] ?? "http://localhost:8188";
});

// 依赖注入
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddScoped<IComfyuiClient, ComfyuiClient>();
builder.Services.AddScoped<ComfyuiProxyService>();
builder.Services.AddSingleton<TaskManager>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

var logger = app.Services.GetRequiredService<Serilog.ILogger>();
var comfyuiOptions = app.Services.GetRequiredService<IOptionsSnapshot<ComfyuiOptions>>();

// 静态文件服务 — 输出文件
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(AppContext.BaseDirectory, "output")),
    RequestPath = "/output"
});

// GET / — 健康概览
app.MapGet("/", async (ComfyuiProxyService service, TaskManager taskMgr) =>
{
    try
    {
        var status = await service.CheckComfyuiStatusAsync();
        return Results.Json(new
        {
            status,
            version = "1.0.0",
            queueLength = taskMgr.QueueLength
        });
    }
    catch
    {
        return Results.Json(new { status = "error", version = "1.0.0", queueLength = taskMgr.QueueLength });
    }
});

// POST /api/generate — 提交生成任务
app.MapPost("/api/generate", async (GenerateRequest request, ComfyuiProxyService service, TaskManager taskMgr) =>
{
    var taskId = taskMgr.Enqueue(request.WorkflowType, request.Prompt);

    try
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var (promptId, error) = await service.SubmitAsync(request);
                if (error != null)
                {
                    logger.Error("提交失败: {Error}", error);
                    taskMgr.Update(taskId, "failed", 0, error: error);
                    return;
                }

                taskMgr.Update(taskId, "running", 10, node: promptId);

                var outputs = await service.PollAsync(promptId);
                var outputPath = await service.DownloadOutputAsync(outputs);

                if (!string.IsNullOrEmpty(outputPath))
                {
                    taskMgr.Update(taskId, "completed", 100, outputPath: outputPath);
                }
                else
                {
                    taskMgr.Update(taskId, "failed", 0, error: "未找到输出文件");
                }
            }
            catch (TimeoutException)
            {
                logger.Error("任务 {TaskId} 超时", taskId);
                taskMgr.Update(taskId, "failed", 0, error: "任务超时");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "任务 {TaskId} 失败", taskId);
                taskMgr.Update(taskId, "failed", 0, error: ex.Message);
            }
        });

        return Results.Json(new GenerateResponse
        {
            TaskId = taskId,
            Status = "queued",
            Progress = 0
        });
    }
    catch (Exception ex)
    {
        taskMgr.Update(taskId, "failed", 0, error: ex.Message);
        return Results.Json(new GenerateResponse
        {
            TaskId = taskId,
            Status = "failed",
            Error = ex.Message
        }, statusCode: 500);
    }
});

// GET /api/tasks — 获取所有任务
app.MapGet("/api/tasks", (TaskManager taskMgr) =>
{
    return Results.Json(taskMgr.All().OrderByDescending(t => t.CreatedAt));
});

// GET /api/tasks/{id} — 获取单个任务
app.MapGet("/api/tasks/{id}", (string id, TaskManager taskMgr) =>
{
    var task = taskMgr.Get(id);
    if (task == null) return Results.NotFound();

    var response = new GenerateResponse
    {
        TaskId = task.Id,
        Status = task.Status,
        Progress = task.Progress,
        Result = task.Result,
        Error = task.Error
    };

    return Results.Json(response);
});

// GET /api/health — 健康检查
app.MapGet("/api/health", async (ComfyuiProxyService service) =>
{
    var comfyuiOk = await service.CheckComfyuiStatusAsync();
    return Results.Json(new HealthResponse
    {
        Status = comfyuiOk ? "ok" : "degraded",
        Version = "1.0.0",
        ComfyuiStatus = comfyuiOk ? "connected" : "disconnected"
    });
});

app.Run();
