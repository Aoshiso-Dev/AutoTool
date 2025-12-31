using System.Drawing;
using System.Runtime.InteropServices;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Infrastructure;

/// <summary>
/// Win32 APIを使用したウィンドウ操作サービスの実装
/// </summary>
public class Win32WindowService : IWindowService
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public IntPtr GetWindowHandle(string? windowTitle, string? windowClassName)
    {
        if (string.IsNullOrEmpty(windowClassName) && string.IsNullOrEmpty(windowTitle))
        {
            return IntPtr.Zero;
        }

        return FindWindow(
            string.IsNullOrEmpty(windowClassName) ? null : windowClassName,
            string.IsNullOrEmpty(windowTitle) ? null : windowTitle);
    }

    public Rectangle? GetWindowRect(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }

        if (GetWindowRect(windowHandle, out var rect))
        {
            return new Rectangle(
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top);
        }

        return null;
    }

    public (int relativeX, int relativeY, bool success, string? errorMessage) ConvertToRelativeCoordinates(
        int absoluteX, int absoluteY, string? windowTitle, string? windowClassName)
    {
        if (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName))
        {
            return (absoluteX, absoluteY, true, null);
        }

        var windowHandle = GetWindowHandle(windowTitle, windowClassName);
        if (windowHandle == IntPtr.Zero)
        {
            return (absoluteX, absoluteY, false, "指定されたウィンドウが見つかりません。");
        }

        var windowRect = GetWindowRect(windowHandle);
        if (windowRect == null)
        {
            return (absoluteX, absoluteY, false, "ウィンドウの位置情報が取得できませんでした。");
        }

        var relativeX = absoluteX - windowRect.Value.Left;
        var relativeY = absoluteY - windowRect.Value.Top;

        return (relativeX, relativeY, true, null);
    }
}
