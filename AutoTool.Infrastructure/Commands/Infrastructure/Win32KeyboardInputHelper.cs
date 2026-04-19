using System.Runtime.InteropServices;
using AutoTool.Commands.Model.Input;

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
        var virtualKey = ToVirtualKey(key);
        if (virtualKey == 0)
        {
            return;
        }

        NativeKeybdEvent((byte)virtualKey, 0, KeyEventfKeyDown, UIntPtr.Zero);
    }

    private static void KeyUpGlobal(CommandKey key)
    {
        var virtualKey = ToVirtualKey(key);
        if (virtualKey == 0)
        {
            return;
        }

        NativeKeybdEvent((byte)virtualKey, 0, KeyEventfKeyUp, UIntPtr.Zero);
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

        var virtualKey = ToVirtualKey(key);
        if (virtualKey != 0)
        {
            NativeSendMessage(hWnd, WmKeyDown, (IntPtr)virtualKey, IntPtr.Zero);
        }
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

        var virtualKey = ToVirtualKey(key);
        if (virtualKey != 0)
        {
            NativeSendMessage(hWnd, WmKeyUp, (IntPtr)virtualKey, IntPtr.Zero);
        }

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

    private static int ToVirtualKey(CommandKey key)
    {
        if (key >= CommandKey.A && key <= CommandKey.Z)
        {
            return 0x41 + (int)(key - CommandKey.A);
        }

        if (key >= CommandKey.D0 && key <= CommandKey.D9)
        {
            return 0x30 + (int)(key - CommandKey.D0);
        }

        if (key >= CommandKey.NumPad0 && key <= CommandKey.NumPad9)
        {
            return 0x60 + (int)(key - CommandKey.NumPad0);
        }

        if (key >= CommandKey.F1 && key <= CommandKey.F24)
        {
            return 0x70 + (int)(key - CommandKey.F1);
        }

        return key switch
        {
            CommandKey.None => 0x00,
            CommandKey.Cancel => 0x03,
            CommandKey.Back => 0x08,
            CommandKey.Tab => 0x09,
            CommandKey.Clear => 0x0C,
            CommandKey.Return or CommandKey.Enter => 0x0D,
            CommandKey.Pause => 0x13,
            CommandKey.Capital => 0x14,
            CommandKey.KanaMode => 0x15,
            CommandKey.JunjaMode => 0x17,
            CommandKey.FinalMode => 0x18,
            CommandKey.KanjiMode => 0x19,
            CommandKey.Escape => 0x1B,
            CommandKey.ImeConvert => 0x1C,
            CommandKey.ImeNonConvert => 0x1D,
            CommandKey.ImeAccept => 0x1E,
            CommandKey.ImeModeChange => 0x1F,
            CommandKey.Space => 0x20,
            CommandKey.Prior => 0x21,
            CommandKey.Next => 0x22,
            CommandKey.End => 0x23,
            CommandKey.Home => 0x24,
            CommandKey.Left => 0x25,
            CommandKey.Up => 0x26,
            CommandKey.Right => 0x27,
            CommandKey.Down => 0x28,
            CommandKey.Select => 0x29,
            CommandKey.Print => 0x2A,
            CommandKey.Execute => 0x2B,
            CommandKey.Snapshot => 0x2C,
            CommandKey.Insert => 0x2D,
            CommandKey.Delete => 0x2E,
            CommandKey.Help => 0x2F,
            CommandKey.LWin => 0x5B,
            CommandKey.RWin => 0x5C,
            CommandKey.Apps => 0x5D,
            CommandKey.Sleep => 0x5F,
            CommandKey.Multiply => 0x6A,
            CommandKey.Add => 0x6B,
            CommandKey.Separator => 0x6C,
            CommandKey.Subtract => 0x6D,
            CommandKey.Decimal => 0x6E,
            CommandKey.Divide => 0x6F,
            CommandKey.NumLock => 0x90,
            CommandKey.Scroll => 0x91,
            CommandKey.LeftShift => 0xA0,
            CommandKey.RightShift => 0xA1,
            CommandKey.LeftCtrl => 0xA2,
            CommandKey.RightCtrl => 0xA3,
            CommandKey.LeftAlt => 0xA4,
            CommandKey.RightAlt => 0xA5,
            CommandKey.BrowserBack => 0xA6,
            CommandKey.BrowserForward => 0xA7,
            CommandKey.BrowserRefresh => 0xA8,
            CommandKey.BrowserStop => 0xA9,
            CommandKey.BrowserSearch => 0xAA,
            CommandKey.BrowserFavorites => 0xAB,
            CommandKey.BrowserHome => 0xAC,
            CommandKey.VolumeMute => 0xAD,
            CommandKey.VolumeDown => 0xAE,
            CommandKey.VolumeUp => 0xAF,
            CommandKey.MediaNextTrack => 0xB0,
            CommandKey.MediaPreviousTrack => 0xB1,
            CommandKey.MediaStop => 0xB2,
            CommandKey.MediaPlayPause => 0xB3,
            CommandKey.LaunchMail => 0xB4,
            CommandKey.SelectMedia => 0xB5,
            CommandKey.LaunchApplication1 => 0xB6,
            CommandKey.LaunchApplication2 => 0xB7,
            CommandKey.Oem1 => 0xBA,
            CommandKey.OemPlus => 0xBB,
            CommandKey.OemComma => 0xBC,
            CommandKey.OemMinus => 0xBD,
            CommandKey.OemPeriod => 0xBE,
            CommandKey.Oem2 => 0xBF,
            CommandKey.Oem3 => 0xC0,
            CommandKey.Oem4 => 0xDB,
            CommandKey.Oem5 => 0xDC,
            CommandKey.Oem6 => 0xDD,
            CommandKey.Oem7 => 0xDE,
            CommandKey.Oem8 => 0xDF,
            CommandKey.Oem102 => 0xE2,
            CommandKey.ProcessKey => 0xE5,
            CommandKey.Packet => 0xE7,
            CommandKey.Attn => 0xF6,
            CommandKey.CrSel => 0xF7,
            CommandKey.ExSel => 0xF8,
            CommandKey.EraseEof => 0xF9,
            CommandKey.Play => 0xFA,
            CommandKey.Zoom => 0xFB,
            CommandKey.NoName => 0xFC,
            CommandKey.Pa1 => 0xFD,
            CommandKey.OemClear => 0xFE,
            _ => 0x00
        };
    }
}
