using System.Runtime.InteropServices;
using AutoTool.Commands.Model.Input;
using System.Windows.Input;

namespace AutoTool.Commands.Infrastructure;

internal static partial class Win32KeyboardInputHelper
{
    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "keybd_event")]
    private static partial void NativeKeybdEvent(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "FindWindowW")]
    private static partial IntPtr NativeFindWindow(string? className, string windowName);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SendMessageW")]
    private static partial IntPtr NativeSendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    private const uint KeyEventfKeyDown = 0x0000;
    private const uint KeyEventfKeyUp = 0x0002;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const int VkMenu = 0x12;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;

    public static void KeyPress(CommandKey key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
    {
        if (string.IsNullOrWhiteSpace(windowTitle) && string.IsNullOrWhiteSpace(windowClassName))
        {
            KeyPressGlobal(key, ctrl, alt, shift);
            return;
        }

        KeyDownToWindow(key, ctrl, alt, shift, windowTitle, windowClassName);
        Thread.Sleep(100);
        KeyUpToWindow(key, ctrl, alt, shift, windowTitle, windowClassName);
    }

    private static void KeyPressGlobal(CommandKey key, bool ctrl, bool alt, bool shift)
    {
        if (ctrl)
        {
            KeyDownGlobal(CommandKey.LeftCtrl);
        }

        if (alt)
        {
            KeyDownGlobal(CommandKey.LeftAlt);
        }

        if (shift)
        {
            KeyDownGlobal(CommandKey.LeftShift);
        }

        KeyDownGlobal(key);
        KeyUpGlobal(key);

        if (ctrl)
        {
            KeyUpGlobal(CommandKey.LeftCtrl);
        }

        if (alt)
        {
            KeyUpGlobal(CommandKey.LeftAlt);
        }

        if (shift)
        {
            KeyUpGlobal(CommandKey.LeftShift);
        }
    }

    private static void KeyDownGlobal(CommandKey key)
    {
        NativeKeybdEvent((byte)KeyInterop.VirtualKeyFromKey(ToWpfKey(key)), 0, KeyEventfKeyDown, UIntPtr.Zero);
    }

    private static void KeyUpGlobal(CommandKey key)
    {
        NativeKeybdEvent((byte)KeyInterop.VirtualKeyFromKey(ToWpfKey(key)), 0, KeyEventfKeyUp, UIntPtr.Zero);
    }

    private static void KeyDownToWindow(CommandKey key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
    {
        var hWnd = NativeFindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastPInvokeError(),
                $"ウィンドウが見つかりません。Title='{windowTitle}', ClassName='{windowClassName}'。");
        }

        if (ctrl)
        {
            NativeSendMessage(hWnd, WmKeyDown, (IntPtr)VkControl, IntPtr.Zero);
        }

        if (alt)
        {
            NativeSendMessage(hWnd, WmKeyDown, (IntPtr)VkMenu, IntPtr.Zero);
        }

        if (shift)
        {
            NativeSendMessage(hWnd, WmKeyDown, (IntPtr)VkShift, IntPtr.Zero);
        }

        NativeSendMessage(hWnd, WmKeyDown, (IntPtr)KeyInterop.VirtualKeyFromKey(ToWpfKey(key)), IntPtr.Zero);
    }

    private static void KeyUpToWindow(CommandKey key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
    {
        var hWnd = NativeFindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastPInvokeError(),
                $"ウィンドウが見つかりません。Title='{windowTitle}', ClassName='{windowClassName}'。");
        }

        NativeSendMessage(hWnd, WmKeyUp, (IntPtr)KeyInterop.VirtualKeyFromKey(ToWpfKey(key)), IntPtr.Zero);

        if (ctrl)
        {
            NativeSendMessage(hWnd, WmKeyUp, (IntPtr)VkControl, IntPtr.Zero);
        }

        if (alt)
        {
            NativeSendMessage(hWnd, WmKeyUp, (IntPtr)VkMenu, IntPtr.Zero);
        }

        if (shift)
        {
            NativeSendMessage(hWnd, WmKeyUp, (IntPtr)VkShift, IntPtr.Zero);
        }
    }

    private static Key ToWpfKey(CommandKey key)
    {
        if (Enum.TryParse<Key>(key.ToString(), ignoreCase: false, out var wpfKey))
        {
            return wpfKey;
        }

        return Key.None;
    }
}
