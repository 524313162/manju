using Microsoft.AspNetCore.Hosting;
using ManjuCraft.Infrastructure.Service;

namespace ManjuCraft.Application.Service
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GetAssetUrl(long projectId, string assetType, long assetId, string viewType)
        {
            return $"asset/{assetType.ToLower()}/{assetId}/{viewType}";
        }

        public async Task<string> SaveAssetAsync(long projectId, string assetType, long assetId, string viewType, byte[] data, string extension)
        {
            var wwwroot = _env.WebRootPath;
            var dirPath = Path.Combine(wwwroot, "asset", assetType.ToLower(), $"{assetId}");
            Directory.CreateDirectory(dirPath);

            var safeExt = extension.StartsWith(".") ? extension : ("." + extension);
            var fileName = viewType + safeExt;
            var filePath = Path.Combine(dirPath, fileName);

            await File.WriteAllBytesAsync(filePath, data);
            return $"asset/{assetType.ToLower()}/{assetId}/{viewType}";
        }
    }
}