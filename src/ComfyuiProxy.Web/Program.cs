using ManjuCraft.Domain.Models;
using ComfyuiProxy.Web.Services;
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
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddScoped<IComfyuiClient, ComfyuiClient>();
builder.Services.AddScoped<ComfyuiProxyService>();
builder.Services.AddSingleton<TaskManager>();
builder.Services.AddSingleton<IFileStorageService, ProxyFileStorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var comfyuiOptions = app.Services.GetRequiredService<IOptionsSnapshot<ComfyuiOptions>>();

// GET / — 健康概览
app.MapGet("/", async (ComfyuiProxyService service, TaskManager taskMgr) =>
{
    try
    {
        var status = await service.CheckComfyuiStatusAsync();
        return Results.Json(new { status, version = "1.0.0", queueLength = taskMgr.QueueLength });
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
                var promptId = await service.SubmitAsync(request.WorkflowType, request.Prompt, request.PositivePrompt, request.ImageUrl);
                taskMgr.Update(taskId, "running", 10, outputPath: promptId);

                var outputs = await service.PollAsync(promptId);
                // PollAsync 返回 Dictionary<string, ComfyuiHistoryNodeOutputs>
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
            catch (Exception ex)
            {
                logger.LogError(ex, "任务 {TaskId} 失败", taskId);
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
    return Results.Json(taskMgr.All());
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
        Result = !string.IsNullOrEmpty(task.OutputPath) ?
            new GenerateResult { Url = task.OutputPath } : null,
        Error = task.Error
    };

    return Results.Json(response);
});

// GET /api/health — 健康检查
app.MapGet("/api/health", () =>
    Results.Json(new HealthResponse { Status = "ok", Version = "1.0.0" }));

app.Run();
