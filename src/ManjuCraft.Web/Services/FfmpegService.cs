using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ManjuCraft.Web.Services;

public interface IFfmpegService
{
    bool IsAvailable { get; }
    string? Version { get; }
    (bool Available, string? Version) Check();
    Task<(bool Success, string Output, string Error, int ExitCode)> MergeVideosAsync(
        string[] inputFiles, string outputFile, string[] shotNames);
    Task<int> GetProgressPercentage();
}

public class FfmpegService : IFfmpegService
{
    private readonly ILogger<FfmpegService> _logger;
    private bool _checked;
    private bool _available;
    private string? _version;
    private int _processPercentage;

    public bool IsAvailable => _available;
    public string? Version => _version;

    public FfmpegService(ILogger<FfmpegService> logger)
    {
        _logger = logger;
    }

    public (bool Available, string? Version) Check()
    {
        if (_checked) return (_available, _version);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                var output = process.StandardOutput.ReadToEnd();
                _version = output.Split('\n').FirstOrDefault()?.Trim() ?? "unknown";
                _available = true;
                _logger.LogInformation("FFmpeg 可用: {Version}", _version);
            }
            else
            {
                _available = false;
                _logger.LogWarning("FFmpeg 检测失败");
            }
        }
        catch
        {
            _available = false;
            _logger.LogWarning("FFmpeg 未找到");
        }

        _checked = true;
        return (_available, _version);
    }

    public async Task<(bool Success, string Output, string Error, int ExitCode)> MergeVideosAsync(
        string[] inputFiles, string outputFile, string[] shotNames)
    {
        _checked = false;
        var (available, version) = Check();
        if (!available)
            return (false, "", "FFmpeg 不可用，请先安装 FFmpeg", -1);

        var listFile = Path.Combine(Path.GetTempPath(), $"manjucraft-list-{Guid.NewGuid()}.txt");
        var lines = inputFiles.Select(f => $"file '{f.Replace("'", "\\'")}'");
        await File.WriteAllTextAsync(listFile, string.Join("\n", lines));

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _logger.LogInformation("开始合并视频: {Count} 个分镜 -> {Output}", inputFiles.Length, outputFile);

        using var process = Process.Start(startInfo);
        var outputTask = process?.StandardError.ReadToEndAsync();
        process?.WaitForExit();

        try { File.Delete(listFile); } catch { }

        var exitCode = process?.ExitCode ?? -1;
        var error = outputTask?.Result ?? "";

        _processPercentage = exitCode == 0 ? 100 : 0;

        if (exitCode == 0)
        {
            _logger.LogInformation("视频合并成功: {Output}", outputFile);
            return (true, outputFile, "", 0);
        }

        _logger.LogError("视频合并失败: {Error}", error);
        return (false, "", error, exitCode);
    }

    public Task<int> GetProgressPercentage()
    {
        return Task.FromResult(_processPercentage);
    }
}
