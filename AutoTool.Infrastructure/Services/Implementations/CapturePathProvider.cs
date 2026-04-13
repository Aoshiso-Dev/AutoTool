using System.IO;
using AutoTool.Core.Ports;
using AutoTool.Panels.Helpers;

namespace AutoTool.Infrastructure.Implementations;

public sealed class CapturePathProvider : ICapturePathProvider
{
    public string CreateCaptureFilePath()
    {
        var appDirectory = ApplicationPathResolver.GetApplicationDirectory();
        var captureDirectory = Path.Combine(appDirectory, "Capture");

        if (!Directory.Exists(captureDirectory))
        {
            Directory.CreateDirectory(captureDirectory);
            System.Diagnostics.Debug.WriteLine($"キャプチャディレクトリを作成: {captureDirectory}");
        }

        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
        var fullPath = Path.Combine(captureDirectory, fileName);
        System.Diagnostics.Debug.WriteLine($"キャプチャファイルパス生成: {fullPath}");
        return fullPath;
    }
}
