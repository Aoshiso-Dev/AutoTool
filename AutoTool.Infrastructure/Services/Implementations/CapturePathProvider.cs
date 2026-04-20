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

    public CapturePathProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string CreateCaptureFilePath()
    {
        var appDirectory = ApplicationPathResolver.GetApplicationDirectory();
        var captureDirectory = Path.Combine(appDirectory, "Capture");

        if (!Directory.Exists(captureDirectory))
        {
            Directory.CreateDirectory(captureDirectory);
            System.Diagnostics.Debug.WriteLine($"キャプチャディレクトリを作成: {captureDirectory}");
        }

        var fileName = $"{_timeProvider.GetLocalNow():yyyyMMddHHmmss}.png";
        var fullPath = Path.Combine(captureDirectory, fileName);
        System.Diagnostics.Debug.WriteLine($"キャプチャファイルパス生成: {fullPath}");
        return fullPath;
    }
}
