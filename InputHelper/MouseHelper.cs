using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Drawing;
using System.Windows;
using System.Diagnostics;

namespace MouseHelper
{

    public static class Input
    {
        #region Win32API
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_HWHEEL = 0x01000;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const uint WM_LBUTTONDOWN = 0x0201;
        const uint WM_LBUTTONUP = 0x0202;
        const int MK_LBUTTON = 0x0001;
        const uint WM_RBUTTONDOWN = 0x0204;
        const uint WM_RBUTTONUP = 0x0205;
        const int MK_RBUTTON = 0x0002;
        const uint WM_MBUTTONDOWN = 0x0207;
        const uint WM_MBUTTONUP = 0x0208;
        const int MK_MBUTTON = 0x0010;
        const uint WM_MOUSEWHEEL = 0x020A;
        const uint WM_MOUSEHWHEEL = 0x020E;
        const uint WM_MOUSEMOVE = 0x0200;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion

        #region AttachInput
        public static void AttachInputToWindow(IntPtr hWnd)
        {
            uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);
            uint currentThreadId = GetCurrentThreadId();

            if (currentThreadId != targetThreadId)
            {
                AttachThreadInput(currentThreadId, targetThreadId, true);
            }
        }

        public static void AttachInputToWindow(string windowTitle)
        {
            var hWnd = FindWindow(null, windowTitle);
            AttachInputToWindow(hWnd);
        }

        public static void DetachInputToWindow(IntPtr hWnd)
        {
            uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);
            uint currentThreadId = GetCurrentThreadId();

            if (currentThreadId != targetThreadId)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }

        public static void DetachInputToWindow(string windowTitle)
        {
            var hWnd = FindWindow(null, windowTitle);
            DetachInputToWindow(hWnd);
        }
        #endregion

        #region ScreenCoordinate
        public static void Click(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void RightClick(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public static void MiddleClick(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
        }

        public static void Wheel(int x, int y, int delta)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, 0);
        }

        public static void HWheel(int x, int y, int delta)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, (uint)delta, 0);
        }

        public static void Move(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void Drag(int x1, int y1, int x2, int y2)
        {
            SetCursorPos(x1, y1);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            SetCursorPos(x2, y2);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void RightDrag(int x1, int y1, int x2, int y2)
        {
            SetCursorPos(x1, y1);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            SetCursorPos(x2, y2);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public static void MiddleDrag(int x1, int y1, int x2, int y2)
        {
            SetCursorPos(x1, y1);
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            SetCursorPos(x2, y2);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
        }

        public static System.Drawing.Point GetCursorPosition()
        {
            System.Drawing.Point lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }
        #endregion

        #region WindowCoordinate（FF14使用不可）
        
        private static IntPtr GetWindowHandle(string windowTitle)
        {
            var hWnd = FindWindow(null, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return hWnd;
        }

        private static RECT GetWindowRect(IntPtr hWnd)
        {
            RECT rect;
            if (!GetWindowRect(hWnd, out rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return rect;
        }

        public static void Click(string windowTitle, int clientX, int clientY)
        {
            var hWnd = GetWindowHandle(windowTitle);
            var rect = GetWindowRect(hWnd);

            IntPtr lParam = (IntPtr)checked((clientY << 16) | (clientX  & 0xFFFF));

            SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }

        public static void RightClick(string windowTitle, int clientX, int clientY)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam = (IntPtr)checked((clientY << 16) | (clientX & 0xFFFF));

            SendMessage(hWnd, WM_RBUTTONDOWN, (IntPtr)MK_RBUTTON, lParam);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_RBUTTONUP, IntPtr.Zero, lParam);
        }

        public static void MiddleClick(string windowTitle, int clientX, int clientY)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam = (IntPtr)checked((clientY << 16) | (clientX & 0xFFFF));

            SendMessage(hWnd, WM_MBUTTONDOWN, (IntPtr)MK_MBUTTON, lParam);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_MBUTTONUP, IntPtr.Zero, lParam);
        }

        public static void Wheel(string windowTitle, int clientX, int clientY, int delta)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam = (IntPtr)checked((clientY << 16) | (clientX & 0xFFFF));

            SendMessage(hWnd, WM_MOUSEWHEEL, (IntPtr)delta, lParam);
        }

        public static void HWheel(string windowTitle, int clientX, int clientY, int delta)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam = (IntPtr)checked((clientY << 16) | (clientX & 0xFFFF));

            SendMessage(hWnd, WM_MOUSEHWHEEL, (IntPtr)delta, lParam);
        }

        public static void Move(string windowTitle, int clientX, int clientY)
        {
            var hWnd = GetWindowHandle(windowTitle);
            var rect = GetWindowRect(hWnd);

            SetCursorPos(rect.Left + clientX, rect.Top + clientY);
        }

        public static void Drag(string windowTitle, int clientX1, int clientY1, int clientX2, int clientY2)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam1 = (IntPtr)checked((clientY1 << 16) | (clientX1 & 0xFFFF));
            IntPtr lParam2 = (IntPtr)checked((clientY2 << 16) | (clientX2 & 0xFFFF));

            SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam1);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam2);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam2);
        }

        public static void RightDrag(string windowTitle, int clientX1, int clientY1, int clientX2, int clientY2)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam1 = (IntPtr)checked((clientY1 << 16) | (clientX1 & 0xFFFF));
            IntPtr lParam2 = (IntPtr)checked((clientY2 << 16) | (clientX2 & 0xFFFF));

            SendMessage(hWnd, WM_RBUTTONDOWN, (IntPtr)MK_RBUTTON, lParam1);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam2);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_RBUTTONUP, IntPtr.Zero, lParam2);
        }

        public static void MiddleDrag(string windowTitle, int clientX1, int clientY1, int clientX2, int clientY2)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam1 = (IntPtr)checked((clientY1 << 16) | (clientX1 & 0xFFFF));
            IntPtr lParam2 = (IntPtr)checked((clientY2 << 16) | (clientX2 & 0xFFFF));

            SendMessage(hWnd, WM_MBUTTONDOWN, (IntPtr)MK_MBUTTON, lParam1);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam2);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_MBUTTONUP, IntPtr.Zero, lParam2);
        }

        public static void WheelDrag(string windowTitle, int clientX1, int clientY1, int clientX2, int clientY2, int delta)
        {
            var hWnd = GetWindowHandle(windowTitle);

            IntPtr lParam1 = (IntPtr)checked((clientY1 << 16) | (clientX1 & 0xFFFF));
            IntPtr lParam2 = (IntPtr)checked((clientY2 << 16) | (clientX2 & 0xFFFF));

            SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam1);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_MOUSEWHEEL, (IntPtr)delta, lParam2);
            Thread.Sleep(100);
            SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam2);
        }

        public static System.Drawing.Point GetCursorPosition(string windowTitle)
        {
            var hWnd = GetWindowHandle(windowTitle);
            var rect = GetWindowRect(hWnd);

            System.Drawing.Point lpPoint;
            GetCursorPos(out lpPoint);

            return new System.Drawing.Point(lpPoint.X - rect.Left, lpPoint.Y - rect.Top);
        }
        
        #endregion
    }

    public static class Event
    {
        #region Win32API
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // フックタイプ（低レベルのマウスフック）
        private const int WH_MOUSE_LL = 14;

        // フックハンドル
        private static IntPtr hookID = IntPtr.Zero;

        // デリゲートを保持するための変数
        private static LowLevelMouseProc proc = HookCallback;

        // マウスイベントのコールバックデリゲート
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_RBUTTONUP = 0x0205;
        const int WM_MBUTTONDOWN = 0x0207;
        const int WM_MBUTTONUP = 0x0208;
        const int WM_MOUSEWHEEL = 0x020A;
        const int WM_MOUSEHWHEEL = 0x020E;
        const int WM_MOUSEMOVE = 0x0200;

        #endregion

        #region Event
        public class MouseEventArgs : EventArgs
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Delta { get; set; }
            public int HWheel { get; set; }

            public MouseEventArgs(int x, int y, int delta, int hWheel)
            {
                X = x;
                Y = y;
                Delta = delta;
                HWheel = hWheel;
            }
        }

        public static EventHandler<MouseEventArgs>? LButtonDown { get; set; }
        public static EventHandler<MouseEventArgs>? LButtonUp { get; set; }
        public static EventHandler<MouseEventArgs>? RButtonDown { get; set; }
        public static EventHandler<MouseEventArgs>? RButtonUp { get; set; }
        public static EventHandler<MouseEventArgs>? MButtonDown { get; set; }
        public static EventHandler<MouseEventArgs>? MButtonUp { get; set; }
        public static EventHandler<MouseEventArgs>? MouseWheel { get; set; }
        public static EventHandler<MouseEventArgs>? MouseHWheel { get; set; }
        public static EventHandler<MouseEventArgs>? MouseMove { get; set; }

        #endregion

        #region Hook
        private static bool SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                if(curProcess?.MainModule == null)
                {
                    return false;
                }

                using (ProcessModule curModule = curProcess.MainModule)
                {
                    hookID = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);

                    return hookID != IntPtr.Zero;
                }
            }
        }

        private static bool Unhook()
        {
            return UnhookWindowsHookEx(hookID);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // nCodeが0以上の場合は、マウスイベントを処理
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                        LButtonDown?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_LBUTTONUP:
                        LButtonUp?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_RBUTTONDOWN:
                        RButtonDown?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_RBUTTONUP:
                        RButtonUp?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_MBUTTONDOWN:
                        MButtonDown?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_MBUTTONUP:
                        MButtonUp?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_MOUSEWHEEL:
                        MouseWheel?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, (int)hookStruct.mouseData, 0));
                        break;
                    case WM_MOUSEHWHEEL:
                        MouseHWheel?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, (int)hookStruct.mouseData));
                        break;
                    case WM_MOUSEMOVE:
                        MouseMove?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                }
            }

            // 次のフックに処理を渡す
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }
        #endregion

        public static void StartHook()
        {
            SetHook(proc);
        }

        public static void StopHook()
        {
            Unhook();
        }
    }
}
