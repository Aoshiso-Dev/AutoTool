using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// 指定座標が属するモニターの DPI を取得する Win32 ヘルパーです。
/// </summary>
public static partial class Win32DpiHelper
{
    private const uint MonitorDefaultToNearest = 2;

    /// <summary>
    /// この機能で扱う状態や種別の選択肢を列挙し、分岐条件を明確にします。
    /// </summary>

    private enum MonitorDpiType
    {
        Effective = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// 処理で利用する値を軽量に保持し、受け渡し時のオーバーヘッドを抑えます。
    /// </summary>

    private struct Point
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll", EntryPoint = "MonitorFromPoint")]
    private static partial IntPtr NativeMonitorFromPoint(Point point, uint flags);

    [LibraryImport("Shcore.dll", EntryPoint = "GetDpiForMonitor")]
    private static partial int NativeGetDpiForMonitor(IntPtr monitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// 指定座標のモニター DPI を返します。取得失敗時は既定の 96 DPI を返します。
    /// </summary>
    public static (double DpiX, double DpiY) GetMonitorDpiAt(int x, int y)
    {
        var monitor = NativeMonitorFromPoint(new Point { X = x, Y = y }, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return (96d, 96d);
        }

        var hr = NativeGetDpiForMonitor(monitor, MonitorDpiType.Effective, out var dpiX, out var dpiY);
        if (hr != 0 || dpiX == 0 || dpiY == 0)
        {
            return (96d, 96d);
        }

        return (dpiX, dpiY);
    }
}
