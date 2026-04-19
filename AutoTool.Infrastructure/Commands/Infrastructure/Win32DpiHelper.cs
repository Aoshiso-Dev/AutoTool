using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

public static partial class Win32DpiHelper
{
    private const uint MonitorDefaultToNearest = 2;

    private enum MonitorDpiType
    {
        Effective = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll", EntryPoint = "MonitorFromPoint")]
    private static partial IntPtr NativeMonitorFromPoint(Point point, uint flags);

    [LibraryImport("Shcore.dll", EntryPoint = "GetDpiForMonitor")]
    private static partial int NativeGetDpiForMonitor(IntPtr monitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

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
