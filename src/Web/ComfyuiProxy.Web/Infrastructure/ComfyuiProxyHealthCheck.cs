using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ComfyuiProxy.Web.Infrastructure;

/// <summary>
/// ComfyUI 健康检查，验证代理服务是否能正常连接到 ComfyUI 服务器
/// </summary>
public class ComfyuiProxyHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ComfyuiProxyHealthCheck> _logger;

    public ComfyuiProxyHealthCheck(
        IConfiguration configuration,
        ILogger<ComfyuiProxyHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comfyuiUrl = _configuration["ComfyUI:Url"] ?? "http://localhost:8188";
            var healthCheckUrl = $"{comfyuiUrl.TrimEnd('/')}/system_info";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await httpClient.GetAsync(healthCheckUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("ComfyUI health check passed");
                return HealthCheckResult.Healthy("ComfyUI 服务可用");
            }
            else
            {
                _logger.LogWarning("ComfyUI health check failed with status: {StatusCode}", response.StatusCode);
                return HealthCheckResult.Unhealthy($"ComfyUI 服务不可用，状态码: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ComfyUI health check error: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy($"ComfyUI 健康检查异常: {ex.Message}");
        }
    }
}