using Microsoft.AspNetCore.Hosting;

namespace ManjuCraft.Application.Service
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GetAssetUrl(long projectId, string assetType, long assetId, string extension)
        {
            return $"asset/{assetType.ToLower()}/{assetId}{extension}";
        }

        public async Task<string> SaveAssetAsync(long projectId, string assetType, long assetId, byte[] data, string extension)
        {
            var wwwroot = _env.WebRootPath;
            var dirPath = Path.Combine(wwwroot, "asset", assetType.ToLower());
            Directory.CreateDirectory(dirPath);

            var safeExt = extension.StartsWith(".") ? extension : ("." + extension);
            var fileName = $"{assetId}{safeExt}";
            var filePath = Path.Combine(dirPath, fileName);

            await File.WriteAllBytesAsync(filePath, data);
            return $"asset/{assetType.ToLower()}/{fileName}";
        }
    }
}