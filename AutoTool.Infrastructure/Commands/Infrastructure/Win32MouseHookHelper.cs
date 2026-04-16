using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

public static class Win32MouseHookHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc callback, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;
    private const int WmMouseMove = 0x0200;

    private static readonly LowLevelMouseProc Proc = HookCallback;
    private static IntPtr _hookId = IntPtr.Zero;

    public sealed class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(int x, int y, int delta, int hWheel)
        {
            X = x;
            Y = y;
            Delta = delta;
            HWheel = hWheel;
        }

        public int X { get; }
        public int Y { get; }
        public int Delta { get; }
        public int HWheel { get; }
    }

    public static event EventHandler<MouseEventArgs>? LButtonDown;
    public static event EventHandler<MouseEventArgs>? LButtonUp;
    public static event EventHandler<MouseEventArgs>? RButtonUp;
    public static event EventHandler<MouseEventArgs>? MouseMove;

    public static void StartHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        if (process.MainModule is null)
        {
            return;
        }

        _hookId = SetWindowsHookEx(WhMouseLl, Proc, GetModuleHandle(process.MainModule.ModuleName), 0);
    }

    public static void StopHook()
    {
        if (_hookId == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var args = new MouseEventArgs(hookStruct.Point.X, hookStruct.Point.Y, 0, 0);

            var handler = ((int)wParam) switch
            {
                WmLButtonDown => LButtonDown,
                WmLButtonUp => LButtonUp,
                WmRButtonUp => RButtonUp,
                WmMouseMove => MouseMove,
                _ => null
            };

            handler?.Invoke(null, args);
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}

