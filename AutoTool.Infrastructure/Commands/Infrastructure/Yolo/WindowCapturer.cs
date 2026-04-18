using System.Drawing;
using AutoTool.Commands.Infrastructure;

namespace YoloWinLib;

internal static class WindowCapturer
{
    /// <summary>
    /// デスクトップ全体（プライマリスクリーン）をキャプチャします。
    /// </summary>
    public static Bitmap CaptureScreen()
    {
        return Win32CaptureCore.CapturePrimaryScreen();
    }

    public static Bitmap? CaptureByTitle(string windowTitle)
    {
        return Win32CaptureCore.TryCaptureWindowByTitle(windowTitle);
    }
}

internal static class DpiUtil
{
    public static void TryEnablePerMonitorDpi()
    {
        Win32CaptureCore.TryEnablePerMonitorDpi();
    }
}
