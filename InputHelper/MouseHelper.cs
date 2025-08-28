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
using System.Net;

namespace MouseHelper
{
    public static class Input
    {
        #region Win32API
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
        #endregion

        #region Action
        private static void PerformMouseClick(int x, int y, uint downEvent, uint upEvent, string windowTitle = "", string windowClassName = "")
        {
            var targetX = x;
            var targetY = y;

            var orgPos = Cursor.GetPos();

            IntPtr hwnd = IntPtr.Zero;
            if (!string.IsNullOrEmpty(windowTitle))
            {
                hwnd = Window.GetHandle(windowTitle, windowClassName);
                var rect = Window.GetRect(hwnd);
                targetX += rect.Left;
                targetY += rect.Top;
                Window.SaveZOrder(hwnd);
                Window.BringToFront(hwnd);
                Thread.Sleep(30);
            }

            Cursor.Lock(targetX, targetY);
            Thread.Sleep(300);
            mouse_event(downEvent, 0, 0, 0, 0);
            Thread.Sleep(30);
            mouse_event(upEvent, 0, 0, 0, 0);
            Thread.Sleep(30);
            Cursor.Unlock();
            Thread.Sleep(30);

            Cursor.SetPos(orgPos.X, orgPos.Y);

            if (hwnd != IntPtr.Zero)
            {
                Window.RestoreZOrder(hwnd);
                Thread.Sleep(30);
            }
        }

        private static void PerformMouseAction(int x, int y, uint actionEvent, int delta, string windowTitle = "", string windowClassName = "")
        {
            var targetX = x;
            var targetY = y;

            var orgPos = Cursor.GetPos();

            IntPtr hwnd = IntPtr.Zero;
            if (!string.IsNullOrEmpty(windowTitle))
            {
                hwnd = Window.GetHandle(windowTitle, windowClassName);
                var rect = Window.GetRect(hwnd);
                targetX += rect.Left;
                targetY += rect.Top;
                Window.SaveZOrder(hwnd);
                Window.BringToFront(hwnd);
                Thread.Sleep(30);
            }

            Cursor.Lock(targetX, targetY);
            mouse_event(actionEvent, 0, 0, (uint)delta, 0);
            Cursor.Unlock();

            Cursor.SetPos(orgPos.X, orgPos.Y);

            if (hwnd != IntPtr.Zero)
            {
                Window.RestoreZOrder(hwnd);
                Thread.Sleep(30);
            }
        }

        private static void PerformDrag(int x1, int y1, int x2, int y2, uint downEvent, uint upEvent, string windowTitle = "", string windowClassName = "")
        {
            var targetX1 = x1;
            var targetY1 = y1;
            var targetX2 = x2;
            var targetY2 = y2;
            var orgPos = Cursor.GetPos();

            IntPtr hwnd = IntPtr.Zero;
            if (!string.IsNullOrEmpty(windowTitle))
            {
                hwnd = Window.GetHandle(windowTitle, windowClassName);
                var rect = Window.GetRect(hwnd);
                targetX1 += rect.Left;
                targetY1 += rect.Top;
                targetX2 += rect.Left;
                targetY2 += rect.Top;
                Window.SaveZOrder(hwnd);
                Window.BringToFront(hwnd);
                Thread.Sleep(30);
            }

            Cursor.Lock(targetX1, targetY1);
            mouse_event(downEvent, 0, 0, 0, 0);
            Cursor.Unlock();
            Thread.Sleep(30);
            Cursor.Lock(targetX2, targetY2);
            mouse_event(upEvent, 0, 0, 0, 0);
            Cursor.Unlock();

            Cursor.SetPos(orgPos.X, orgPos.Y);

            if (hwnd != IntPtr.Zero)
            {
                Window.RestoreZOrder(hwnd);
                Thread.Sleep(30);
            }
        }

        private static void PerformMove(int x, int y, string windowTitle = "", string windowClassName = "") => Cursor.SetPos(x, y, windowTitle, windowClassName);

        #endregion

        public static void Click(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClick(x, y, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName);

        public static void RightClick(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClick(x, y, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName);

        public static void MiddleClick(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClick(x, y, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName);

        public static void Wheel(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
            => PerformMouseAction(x, y, MOUSEEVENTF_WHEEL, delta, windowTitle, windowClassName);

        public static void HWheel(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
            => PerformMouseAction(x, y, MOUSEEVENTF_HWHEEL, delta, windowTitle, windowClassName);

        public static void Move(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMove(x, y, windowTitle, windowClassName);

        public static void Drag(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDrag(x1, y1, x2, y2, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName);

        public static void RightDrag(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDrag(x1, y1, x2, y2, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName);

        public static void MiddleDrag(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDrag(x1, y1, x2, y2, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName);
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

        public static void StartHook() => SetHook(proc);
        public static void StopHook() => Unhook();
    }

    public static class Block
    {
        #region Win32API
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

        #region Hook
        private static bool SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                if (curProcess?.MainModule == null)
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
            // マウスイベントを全てブロック
            if (nCode >= 0)
            {
                return IntPtr.Zero;
                /*
            switch ((int)wParam)
            {
                case WM_LBUTTONDOWN: return IntPtr.Zero;
                case WM_LBUTTONUP: return IntPtr.Zero;
                case WM_RBUTTONDOWN: return IntPtr.Zero;
                case WM_RBUTTONUP: return IntPtr.Zero;
                case WM_MBUTTONDOWN: return IntPtr.Zero;
                case WM_MBUTTONUP: return IntPtr.Zero;
                case WM_MOUSEWHEEL: return IntPtr.Zero;
                case WM_MOUSEHWHEEL: return IntPtr.Zero;
                case WM_MOUSEMOVE: return IntPtr.Zero;
                }
                */
            }

            // 次のフックに処理を渡す
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }
        #endregion

        public static void StartBlock() => SetHook(proc);
     
        public static void StopBlock() => Unhook();
    }

    public static class Cursor
    {
        #region Win32API
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetCursorPos(out System.Drawing.Point lpPoint);
        #endregion

        #region Pos
        public static System.Drawing.Point GetPos(string windowTitle = "", string windowClassName = "")
        {
            var lpPoint = new System.Drawing.Point();

            if (windowTitle == "")
            {
                GetCursorPos(out lpPoint);
                return lpPoint;
            }
            else
            {
                var hWnd = Window.GetHandle(windowTitle, windowClassName);
                var rect = Window.GetRect(hWnd);
                GetCursorPos(out lpPoint);
                return new System.Drawing.Point(lpPoint.X - rect.Left, lpPoint.Y - rect.Top);
            }
        }

        public static void SetPos(int x, int y, string windowTitle = "", string windowClassName = "")
        {
            if (windowTitle == "")
            {
                SetCursorPos(x, y);
            }
            else
            {
                var hWnd = Window.GetHandle(windowTitle, windowClassName);
                var rect = Window.GetRect(hWnd);
                SetCursorPos(rect.Left + x, rect.Top + y);
            }
        }
        #endregion

        #region Locker
        private static bool isLocked = true;

        public static void Lock(int x, int y)
        {
            isLocked = true;
            Thread mouseMoveThread = new Thread(() =>
            {
                while (isLocked)
                {
                    SetPos(x, y);
                    Thread.Sleep(10);
                }
            });
            mouseMoveThread.Start();
        }

        public static void Unlock()
        {
            isLocked = false;
        }
        #endregion
    }

    internal static class Window
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
        public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const uint GW_HWNDPREV = 3;
        private const uint GW_HWNDNEXT = 2;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOREDRAW = 0x0008;
        private const uint SWP_NOACTIVATE = 0x0010;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        #endregion

        #region Handle
        public static IntPtr GetHandle(string windowTitle, string windowClassName)
        {
            var hWnd = FindWindow(string.IsNullOrEmpty(windowClassName) ? null : windowClassName, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            return hWnd;
        }
        #endregion

        #region Rect
        public static RECT GetRect(IntPtr hWnd)
        {
            if (!GetWindowRect(hWnd, out RECT rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            return rect;
        }
        #endregion

        #region ZOrder
        private static IntPtr originalNextWindow = IntPtr.Zero;
        private static IntPtr originalPrevWindow = IntPtr.Zero;

        public static void SaveZOrder(IntPtr targetWindow)
        {
            originalNextWindow = GetWindow(targetWindow, GW_HWNDNEXT);
            originalPrevWindow = GetWindow(targetWindow, GW_HWNDPREV);
        }

        // 特定ウィンドウをフォアグラウンドに設定
        public static void BringToFront(IntPtr targetWindow)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            uint targetThreadId = GetWindowThreadProcessId(targetWindow, out _);
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);

            if (targetThreadId != foregroundThreadId)
            {
                // フォーカスを同期
                AttachThreadInput(foregroundThreadId, targetThreadId, true);
            }

            // ウィンドウを一時的にTopMostに設定してから解除
            SetWindowPos(targetWindow, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            SetWindowPos(targetWindow, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            SetForegroundWindow(targetWindow);
            SetWindowPos(targetWindow, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            if (targetThreadId != foregroundThreadId)
            {
                AttachThreadInput(foregroundThreadId, targetThreadId, false);
            }
        }

        // 特定ウィンドウを元のZオーダーに復元
        public static void RestoreZOrder(IntPtr targetWindow)
        {
            if (originalNextWindow != IntPtr.Zero)
            {
                // 元の次のウィンドウの後に配置
                SetWindowPos(targetWindow, originalNextWindow, 0, 0, 0, 0,
                             SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            else if (originalPrevWindow != IntPtr.Zero)
            {
                // 元の前のウィンドウの前に配置
                SetWindowPos(targetWindow, originalPrevWindow, 0, 0, 0, 0,
                             SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            else
            {
                // 前後の情報がない場合はトップに配置
                SetWindowPos(targetWindow, HWND_TOP, 0, 0, 0, 0,
                             SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }
        #endregion
    }
}
