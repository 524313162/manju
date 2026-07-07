using ComfyuiProxy.Web.ComfyFlows;
using ComfyuiProxy.Web.Infrastructure;
using ComfyuiProxy.Web.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ComfyUI 代理 API", Version = "v1" });
});

// 配置 HttpClient（供 Agent 使用）
builder.Services.AddHttpClient("http", ext =>
{
    ext.Timeout = TimeSpan.FromMinutes(5);
})
.AddPolicyHandler(GetRetryPolicy());

// 注册 ComfyUI 代理服务
builder.Services.AddSingleton<ComfyuiProxyService>();

// 注册所有 Agent
builder.Services.AddTransient<ZImageTextToImageAgent>();
builder.Services.AddTransient<ZImageCharacterProfileAgent>();
builder.Services.AddTransient<LtxTextToVideoAgent>();
builder.Services.AddTransient<LtxImageToVideoAgent>();
builder.Services.AddTransient<HiDreamStoryboardAgent>();
builder.Services.AddTransient<AceMusicAgent>();
builder.Services.AddTransient<StableBgmAgent>();
builder.Services.AddTransient<LlmQwenAgent>();

// 注册 Agent 工厂
builder.Services.AddSingleton<ComfyUIAgentFactory>();

// 配置健康检查
builder.Services.AddHealthChecks()
.AddCheck<ComfyuiProxyHealthCheck>("comfyui_health_check");

var app = builder.Build();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ComfyUI 代理 API v1"));
}

// 添加中间件，在非开发环境时返回系统时间
app.Use(async (context, next) =>
{
    var environment = context.RequestServices.GetRequiredService<IConfiguration>()["ASPNETCORE_ENVIRONMENT"];
    if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) && context.Request.Path == "/")
    {
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        return;
    }
    await next();
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests || !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, delay, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} of 3 due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
            });
}
