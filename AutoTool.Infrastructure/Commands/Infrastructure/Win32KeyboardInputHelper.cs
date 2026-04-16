using System.Runtime.InteropServices;
using System.Windows.Input;

namespace AutoTool.Commands.Infrastructure;

internal static class Win32KeyboardInputHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string windowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

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
        keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KeyEventfKeyDown, UIntPtr.Zero);
    }

    private static void KeyUpGlobal(Key key)
    {
        keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KeyEventfKeyUp, UIntPtr.Zero);
    }

    private static void KeyDownToWindow(Key key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
    {
        var hWnd = FindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Window not found. Title='{windowTitle}', ClassName='{windowClassName}'.");
        }

        if (ctrl)
        {
            SendMessage(hWnd, WmKeyDown, (IntPtr)VkControl, IntPtr.Zero);
        }

        if (alt)
        {
            SendMessage(hWnd, WmKeyDown, (IntPtr)VkMenu, IntPtr.Zero);
        }

        if (shift)
        {
            SendMessage(hWnd, WmKeyDown, (IntPtr)VkShift, IntPtr.Zero);
        }

        SendMessage(hWnd, WmKeyDown, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);
    }

    private static void KeyUpToWindow(Key key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
    {
        var hWnd = FindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Window not found. Title='{windowTitle}', ClassName='{windowClassName}'.");
        }

        SendMessage(hWnd, WmKeyUp, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);

        if (ctrl)
        {
            SendMessage(hWnd, WmKeyUp, (IntPtr)VkControl, IntPtr.Zero);
        }

        if (alt)
        {
            SendMessage(hWnd, WmKeyUp, (IntPtr)VkMenu, IntPtr.Zero);
        }

        if (shift)
        {
            SendMessage(hWnd, WmKeyUp, (IntPtr)VkShift, IntPtr.Zero);
        }
    }
}

