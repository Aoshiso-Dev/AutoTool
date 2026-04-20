using System.Drawing;
using AutoTool.Commands.Infrastructure;

namespace AutoTool.Infrastructure.Vision.Yolo;

/// <summary>
/// 対象ウィンドウを画像として取得し、推論入力に使えるフレームを生成します。
/// </summary>
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

/// <summary>
/// DPI スケーリング値の取得と座標変換補助を行い、高 DPI 環境でも正しい座標処理を維持します。
/// </summary>
internal static class DpiUtil
{
    public static void TryEnablePerMonitorDpi()
    {
        Win32CaptureCore.TryEnablePerMonitorDpi();
    }
}
