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

// 配置 HttpClient
builder.Services.AddHttpClient<ComfyuiProxyService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddPolicyHandler(GetRetryPolicy());

// 配置服务
builder.Services.AddSingleton<ComfyuiProxyService>();

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 配置健康检查端点
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new() { Predicate = _ => false });
app.MapHealthChecks("/health/live", new() { Predicate = _ => true });

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
