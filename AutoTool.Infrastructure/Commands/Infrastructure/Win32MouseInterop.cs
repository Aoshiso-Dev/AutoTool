using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

public static partial class Win32MouseInterop
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

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "mouse_event")]
    private static partial void NativeMouseEvent(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeGetCursorPos(out POINT point);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeSetCursorPos(int x, int y);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "FindWindowW")]
    private static partial IntPtr NativeFindWindow(string? className, string windowName);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeGetWindowRect(IntPtr hWnd, out RECT rect);

    [LibraryImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeSetForegroundWindow(IntPtr hWnd);

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
        Win32NativeGuards.ThrowIfFalse(NativeGetCursorPos(out var point), nameof(NativeGetCursorPos));

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

        Win32NativeGuards.ThrowIfFalse(NativeGetCursorPos(out var originalPos), nameof(NativeGetCursorPos));

        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            var hWnd = Win32NativeGuards.ThrowIfZero(
                NativeFindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle),
                nameof(NativeFindWindow),
                $"Window not found: {windowTitle}");

            Win32NativeGuards.ThrowIfFalse(NativeGetWindowRect(hWnd, out var rect), nameof(NativeGetWindowRect));

            targetX += rect.Left;
            targetY += rect.Top;
            NativeSetForegroundWindow(hWnd);
            await Task.Delay(30).ConfigureAwait(false);
        }

        try
        {
            NativeSetCursorPos(targetX, targetY);
            await Task.Delay(30).ConfigureAwait(false);

            NativeMouseEvent(downEvent, 0, 0, 0, 0);
            await Task.Delay(20).ConfigureAwait(false);
            NativeMouseEvent(upEvent, 0, 0, 0, 0);
        }
        finally
        {
            NativeSetCursorPos(originalPos.X, originalPos.Y);
        }
    }
}
