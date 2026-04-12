using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoTool.Commands.Infrastructure;

public static class Win32WindowInfoHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

    public static string GetWindowTitle(Point point)
    {
        var hWnd = GetWindowHandle(point);
        var text = new StringBuilder(256);
        GetWindowText(hWnd, text, text.Capacity);
        return text.ToString();
    }

    public static string GetWindowClassName(Point point)
    {
        var hWnd = GetWindowHandle(point);
        var className = new StringBuilder(256);
        GetClassName(hWnd, className, className.Capacity);
        return className.ToString();
    }

    private static IntPtr GetWindowHandle(Point point)
    {
        var hWnd = WindowFromPoint(new POINT { X = point.X, Y = point.Y });
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        return hWnd;
    }
}

