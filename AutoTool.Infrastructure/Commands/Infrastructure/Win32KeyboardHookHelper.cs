using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// 低レベル API 呼び出しをラップして共通化し、呼び出し側の実装を簡潔にします。
/// </summary>

public static partial class Win32KeyboardHookHelper
{
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// 処理で利用する値を軽量に保持し、受け渡し時のオーバーヘッドを抑えます。
    /// </summary>

    private struct KbdLlHookStruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowsHookExW")]
    private static partial IntPtr NativeSetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "UnhookWindowsHookEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeUnhookWindowsHookEx(IntPtr hook);

    [LibraryImport("user32.dll", EntryPoint = "CallNextHookEx")]
    private static partial IntPtr NativeCallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "GetModuleHandleW")]
    private static partial IntPtr NativeGetModuleHandle(string moduleName);

    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private static readonly LowLevelKeyboardProc Proc = HookCallback;
    private static readonly object Gate = new();
    private static IntPtr _hookId = IntPtr.Zero;
    private static int _hookUserCount;

    /// <summary>
    /// イベント発生時に通知する追加情報を保持し、受信側が状態を判断できるようにします。
    /// </summary>
    public sealed class KeyboardHookEventArgs(int virtualKey, bool isKeyDown) : EventArgs
    {
        public int VirtualKey { get; } = virtualKey;
        public bool IsKeyDown { get; } = isKeyDown;
    }

    public static event EventHandler<KeyboardHookEventArgs>? KeyChanged;

    public static void StartHook()
    {
        lock (Gate)
        {
            _hookUserCount++;
            if (_hookId != IntPtr.Zero)
            {
                return;
            }
        }

        using var process = Process.GetCurrentProcess();
        if (process.MainModule is null)
        {
            lock (Gate)
            {
                _hookUserCount = Math.Max(0, _hookUserCount - 1);
            }
            return;
        }

        var hookId = NativeSetWindowsHookEx(WhKeyboardLl, Proc, NativeGetModuleHandle(process.MainModule.ModuleName), 0);
        lock (Gate)
        {
            if (hookId == IntPtr.Zero)
            {
                _hookUserCount = Math.Max(0, _hookUserCount - 1);
                return;
            }

            _hookId = hookId;
        }
    }

    public static void StopHook()
    {
        IntPtr hookToRelease = IntPtr.Zero;

        lock (Gate)
        {
            if (_hookUserCount > 0)
            {
                _hookUserCount--;
            }

            if (_hookUserCount != 0 || _hookId == IntPtr.Zero)
            {
                return;
            }

            hookToRelease = _hookId;
            _hookId = IntPtr.Zero;
        }

        NativeUnhookWindowsHookEx(hookToRelease);
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = (int)wParam;
            if (message is WmKeyDown or WmKeyUp or WmSysKeyDown or WmSysKeyUp)
            {
                var hookStruct = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                var virtualKey = (int)hookStruct.VkCode;
                var isKeyDown = message is WmKeyDown or WmSysKeyDown;
                KeyChanged?.Invoke(null, new KeyboardHookEventArgs(virtualKey, isKeyDown));
            }
        }

        return NativeCallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
