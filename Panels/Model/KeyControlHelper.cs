using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

internal class KeyControlHelper
{
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static void KeyDown(System.Windows.Input.Key key)
    {
        keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
    }

    public static void KeyUp(System.Windows.Input.Key key)
    {
        keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void KeyPress(System.Windows.Input.Key key)
    {
        KeyDown(key);
        KeyUp(key);
    }

    public static void KeyPress(System.Windows.Input.Key key, bool ctrl, bool alt, bool shift)
    {
        if (ctrl) KeyDown(Key.LeftCtrl);
        if (alt) KeyDown(Key.LeftAlt);
        if (shift) KeyDown(Key.LeftShift);

        KeyPress(key);

        if (ctrl) KeyUp(Key.LeftCtrl);
        if (alt) KeyUp(Key.LeftAlt);
        if (shift) KeyUp(Key.LeftShift);
    }
}