using System.IO;
using AutoTool.Application.Ports;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Infrastructure.Implementations;

/// <summary>
/// 呼び出し元が必要とする値やパスを取得して提供します。
/// </summary>
public sealed class CapturePathProvider : ICapturePathProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogWriter? _logWriter;

    public CapturePathProvider(ILogWriter? logWriter = null, TimeProvider? timeProvider = null)
    {
        _logWriter = logWriter;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string CreateCaptureFilePath()
    {
        var appDirectory = ApplicationPathResolver.GetApplicationDirectory();
        var captureDirectory = Path.Combine(appDirectory, "Capture");

        if (!Directory.Exists(captureDirectory))
        {
            Directory.CreateDirectory(captureDirectory);
            _logWriter?.WriteStructured(
                "Capture",
                "CreateCaptureDirectory",
                new Dictionary<string, object?>
                {
                    ["Directory"] = captureDirectory
                });
        }

        var fileName = $"{_timeProvider.GetLocalNow():yyyyMMddHHmmss}.png";
        var fullPath = Path.Combine(captureDirectory, fileName);
        _logWriter?.WriteStructured(
            "Capture",
            "CreateCaptureFilePath",
            new Dictionary<string, object?>
            {
                ["Path"] = fullPath
            });
        return fullPath;
    }
}
