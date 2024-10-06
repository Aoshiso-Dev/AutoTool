using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Drawing;
using System.Windows;


namespace KeyHelper
{
    public static class Input
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

        const int VK_MENU = 0x12;     // ALT
        const int VK_CONTROL = 0x11;  // CTRL
        const int VK_SHIFT = 0x10;    // SHIFT

        #region GlobalHotkey
        private static void KeyDown(System.Windows.Input.Key key)
        {
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        }

        private static void KeyUp(System.Windows.Input.Key key)
        {
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static void KeyPress(System.Windows.Input.Key key)
        {
            KeyDown(key);
            KeyUp(key);
        }

        public static void KeyPress(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            if (ctrl) KeyDown(Key.LeftCtrl);
            if (alt) KeyDown(Key.LeftAlt);
            if (shift) KeyDown(Key.LeftShift);

            KeyPress(key);

            if (ctrl) KeyUp(Key.LeftCtrl);
            if (alt) KeyUp(Key.LeftAlt);
            if (shift) KeyUp(Key.LeftShift);
        }
        #endregion

        #region WindowHotkey

        private static void KeyDown(string windowTitle, System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            var hWnd = FindWindow(null, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            if (ctrl) SendMessage(hWnd, WM_KEYDOWN, VK_CONTROL, IntPtr.Zero);
            if (alt) SendMessage(hWnd, WM_KEYDOWN, VK_MENU, IntPtr.Zero);
            if (shift) SendMessage(hWnd, WM_KEYDOWN, VK_SHIFT, IntPtr.Zero);

            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);
        }
        private static void KeyUp(string windowTitle, System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            var hWnd = FindWindow(null, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            SendMessage(hWnd, WM_KEYUP, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);

            if (ctrl) SendMessage(hWnd, WM_KEYUP, VK_CONTROL, IntPtr.Zero);
            if (alt) SendMessage(hWnd, WM_KEYUP, VK_MENU, IntPtr.Zero);
            if (shift) SendMessage(hWnd, WM_KEYUP, VK_SHIFT, IntPtr.Zero);
        }

        public static void KeyPress(string windowTitle, System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            KeyDown(windowTitle, key, ctrl, alt, shift);
            Thread.Sleep(100);
            KeyUp(windowTitle, key, ctrl, alt, shift);
        }
        #endregion
    }
}
