using Microsoft.AspNetCore.Hosting;
using ManjuCraft.Infrastructure.Service;

namespace ComfyuiProxy.Web.Services;

/// <summary>
/// 代理服务的文件存储 — 将生成结果保存到本地目录
/// </summary>
public class ProxyFileStorageService : IFileStorageService
{
    private readonly string _outputDir;
    private readonly string _baseUrl;

    public ProxyFileStorageService(IWebHostEnvironment env)
    {
        _outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(_outputDir);
        _baseUrl = "http://localhost:5212";
    }

    public async Task<string> SaveAssetAsync(long projectId, string entityType, long entityId, string viewType, byte[] data, string extension)
    {
        var dir = Path.Combine(_outputDir, entityType, $"{entityId}");
        Directory.CreateDirectory(dir);
        var fileName = $"{viewType}_{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(dir, fileName);

        await File.WriteAllBytesAsync(filePath, data);
        return GetAssetUrl(projectId, entityType, entityId, viewType);
    }

    public string GetAssetUrl(long projectId, string entityType, long entityId, string viewType)
    {
        return $"/output/{entityType}/{entityId}/{viewType}_output";
    }
}
