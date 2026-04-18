using System.Runtime.InteropServices;
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

    public static void KeyPress(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
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

    private static void KeyPressGlobal(Key key, bool ctrl, bool alt, bool shift)
    {
        if (ctrl)
        {
            KeyDownGlobal(Key.LeftCtrl);
        }

        if (alt)
        {
            KeyDownGlobal(Key.LeftAlt);
        }

        if (shift)
        {
            KeyDownGlobal(Key.LeftShift);
        }

        KeyDownGlobal(key);
        KeyUpGlobal(key);

        if (ctrl)
        {
            KeyUpGlobal(Key.LeftCtrl);
        }

        if (alt)
        {
            KeyUpGlobal(Key.LeftAlt);
        }

        if (shift)
        {
            KeyUpGlobal(Key.LeftShift);
        }
    }

    private static void KeyDownGlobal(Key key)
    {
        NativeKeybdEvent((byte)KeyInterop.VirtualKeyFromKey(key), 0, KeyEventfKeyDown, UIntPtr.Zero);
    }

    private static void KeyUpGlobal(Key key)
    {
        NativeKeybdEvent((byte)KeyInterop.VirtualKeyFromKey(key), 0, KeyEventfKeyUp, UIntPtr.Zero);
    }

    private static void KeyDownToWindow(Key key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
    {
        var hWnd = NativeFindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastPInvokeError(),
                $"Window not found. Title='{windowTitle}', ClassName='{windowClassName}'.");
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

        NativeSendMessage(hWnd, WmKeyDown, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);
    }

    private static void KeyUpToWindow(Key key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
    {
        var hWnd = NativeFindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastPInvokeError(),
                $"Window not found. Title='{windowTitle}', ClassName='{windowClassName}'.");
        }

        NativeSendMessage(hWnd, WmKeyUp, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);

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
}
