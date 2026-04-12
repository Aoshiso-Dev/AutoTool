using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

public static class Win32MouseInterop
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    public static Task ClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
    {
        return PerformClickAsync(x, y, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName);
    }

    public static System.Drawing.Point GetCursorPosition()
    {
        if (!GetCursorPos(out var point))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        return new System.Drawing.Point(point.X, point.Y);
    }

    public static Task RightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
    {
        return PerformClickAsync(x, y, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName);
    }

    public static Task MiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
    {
        return PerformClickAsync(x, y, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName);
    }

    private static async Task PerformClickAsync(
        int x,
        int y,
        uint downEvent,
        uint upEvent,
        string windowTitle,
        string windowClassName)
    {
        var targetX = x;
        var targetY = y;

        if (!GetCursorPos(out var originalPos))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            var hWnd = FindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), $"Window not found: {windowTitle}");
            }

            if (!GetWindowRect(hWnd, out var rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            targetX += rect.Left;
            targetY += rect.Top;
            SetForegroundWindow(hWnd);
            await Task.Delay(30).ConfigureAwait(false);
        }

        try
        {
            SetCursorPos(targetX, targetY);
            await Task.Delay(30).ConfigureAwait(false);

            mouse_event(downEvent, 0, 0, 0, 0);
            await Task.Delay(20).ConfigureAwait(false);
            mouse_event(upEvent, 0, 0, 0, 0);
        }
        finally
        {
            SetCursorPos(originalPos.X, originalPos.Y);
        }
    }
}

