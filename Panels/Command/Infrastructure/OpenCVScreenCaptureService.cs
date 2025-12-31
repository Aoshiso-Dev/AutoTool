using MacroPanels.Command.Services;
using OpenCVHelper;

namespace MacroPanels.Command.Infrastructure;

/// <summary>
/// OpenCVを使用したスクリーンキャプチャサービスの実装
/// </summary>
public class OpenCVScreenCaptureService : IScreenCaptureService
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
                ? ScreenCaptureHelper.CaptureScreen()
                : ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);

            if (cancellationToken.IsCancellationRequested) return;

            ScreenCaptureHelper.SaveCapture(mat, savePath);
        }, cancellationToken);
    }

    public Task CaptureToFileAsync(string filePath, string? windowTitle = null, string? windowClassName = null, CancellationToken cancellationToken = default)
    {
        return SaveScreenshotAsync(filePath, windowTitle, windowClassName, cancellationToken);
    }
}
