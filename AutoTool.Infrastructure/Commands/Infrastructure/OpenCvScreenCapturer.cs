using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// OpenCVを使用したスクリーンキャプチャの実装
/// </summary>
public class OpenCvScreenCapturer : IScreenCapturer
{
    public Task SaveScreenshotAsync(string savePath, string? windowTitle = null, string? windowClassName = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var dir = System.IO.Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            using var mat = (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName))
                ? Win32ScreenCaptureHelper.CaptureScreen()
                : Win32ScreenCaptureHelper.CaptureWindow(windowTitle ?? string.Empty, windowClassName ?? string.Empty);

            if (cancellationToken.IsCancellationRequested) return;

            Win32ScreenCaptureHelper.SaveCapture(mat, savePath);
        }, cancellationToken);
    }

    public Task CaptureToFileAsync(string filePath, string? windowTitle = null, string? windowClassName = null, CancellationToken cancellationToken = default)
    {
        return SaveScreenshotAsync(filePath, windowTitle, windowClassName, cancellationToken);
    }
}


