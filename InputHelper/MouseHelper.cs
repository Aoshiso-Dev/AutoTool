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
using System.IO;
using LogHelper;
using System.Collections.Concurrent;

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
        private static async Task PerformMouseClickAsync(int x, int y, uint downEvent, uint upEvent, string windowTitle = "", string windowClassName = "")
        {
            var targetX = x;
            var targetY = y;
            var orgPos = Cursor.GetPos();

            IntPtr hwnd = IntPtr.Zero;
            WindowZOrderInfo? zOrderInfo = null;

            try
            {
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    hwnd = Window.GetHandle(windowTitle, windowClassName);
                    var rect = Window.GetRect(hwnd);
                    targetX += rect.Left;
                    targetY += rect.Top;
                    
                    // Zオーダー情報を保存
                    zOrderInfo = Window.SaveZOrder(hwnd);
                    Window.BringToFront(hwnd);
                    await Task.Delay(30); // 非同期待機
                }

                Cursor.Lock(targetX, targetY);
                await Task.Delay(300);
                
                mouse_event(downEvent, 0, 0, 0, 0);
                await Task.Delay(30);
                mouse_event(upEvent, 0, 0, 0, 0);
                await Task.Delay(30);
                
                Cursor.Unlock();
                await Task.Delay(30);

                Cursor.SetPos(orgPos.X, orgPos.Y);

                // Zオーダーを復元
                if (hwnd != IntPtr.Zero && zOrderInfo != null)
                {
                    Window.RestoreZOrder(hwnd, zOrderInfo);
                    await Task.Delay(30);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformMouseClick", $"Error: {ex.Message}");
                
                // エラー時もカーソルとZオーダーを復元
                try
                {
                    Cursor.Unlock();
                    Cursor.SetPos(orgPos.X, orgPos.Y);
                    if (hwnd != IntPtr.Zero && zOrderInfo != null)
                    {
                        Window.RestoreZOrder(hwnd, zOrderInfo);
                    }
                }
                catch { /* 復元エラーは無視 */ }
                
                throw;
            }

            // ログ出力
            var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var methodName = System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "Unknown";
            var resultMessage = $"Click: {targetX}, {targetY}";
            if (hwnd != IntPtr.Zero)
            {
                resultMessage += $" ({windowTitle}[{windowClassName}] {x},{y})";
            }
            GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);
        }

        private static async Task PerformMouseActionAsync(int x, int y, uint actionEvent, int delta, string windowTitle = "", string windowClassName = "")
        {
            var targetX = x;
            var targetY = y;
            var orgPos = Cursor.GetPos();

            IntPtr hwnd = IntPtr.Zero;
            WindowZOrderInfo? zOrderInfo = null;

            try
            {
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    hwnd = Window.GetHandle(windowTitle, windowClassName);
                    var rect = Window.GetRect(hwnd);
                    targetX += rect.Left;
                    targetY += rect.Top;
                    zOrderInfo = Window.SaveZOrder(hwnd);
                    Window.BringToFront(hwnd);
                    await Task.Delay(30);
                }

                Cursor.Lock(targetX, targetY);
                mouse_event(actionEvent, 0, 0, (uint)delta, 0);
                Cursor.Unlock();

                Cursor.SetPos(orgPos.X, orgPos.Y);

                if (hwnd != IntPtr.Zero && zOrderInfo != null)
                {
                    Window.RestoreZOrder(hwnd, zOrderInfo);
                    await Task.Delay(30);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformMouseAction", $"Error: {ex.Message}");
                
                try
                {
                    Cursor.Unlock();
                    Cursor.SetPos(orgPos.X, orgPos.Y);
                    if (hwnd != IntPtr.Zero && zOrderInfo != null)
                    {
                        Window.RestoreZOrder(hwnd, zOrderInfo);
                    }
                }
                catch { }
                
                throw;
            }

            // ログ出力
            var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var methodName = System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "Unknown";
            var resultMessage = $"Wheel: {targetX}, {targetY}";
            if (hwnd != IntPtr.Zero)
            {
                resultMessage += $" ({windowTitle}[{windowClassName}] {x},{y})";
            }
            GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);
        }

        private static async Task PerformDragAsync(int x1, int y1, int x2, int y2, uint downEvent, uint upEvent, string windowTitle = "", string windowClassName = "")
        {
            var targetX1 = x1;
            var targetY1 = y1;
            var targetX2 = x2;
            var targetY2 = y2;
            var orgPos = Cursor.GetPos();

            IntPtr hwnd = IntPtr.Zero;
            WindowZOrderInfo? zOrderInfo = null;

            try
            {
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    hwnd = Window.GetHandle(windowTitle, windowClassName);
                    var rect = Window.GetRect(hwnd);
                    targetX1 += rect.Left;
                    targetY1 += rect.Top;
                    targetX2 += rect.Left;
                    targetY2 += rect.Top;
                    zOrderInfo = Window.SaveZOrder(hwnd);
                    Window.BringToFront(hwnd);
                    await Task.Delay(30);
                }

                Cursor.Lock(targetX1, targetY1);
                mouse_event(downEvent, 0, 0, 0, 0);
                Cursor.Unlock();
                await Task.Delay(30);
                
                Cursor.Lock(targetX2, targetY2);
                mouse_event(upEvent, 0, 0, 0, 0);
                Cursor.Unlock();

                Cursor.SetPos(orgPos.X, orgPos.Y);

                if (hwnd != IntPtr.Zero && zOrderInfo != null)
                {
                    Window.RestoreZOrder(hwnd, zOrderInfo);
                    await Task.Delay(30);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformDrag", $"Error: {ex.Message}");
                
                try
                {
                    Cursor.Unlock();
                    Cursor.SetPos(orgPos.X, orgPos.Y);
                    if (hwnd != IntPtr.Zero && zOrderInfo != null)
                    {
                        Window.RestoreZOrder(hwnd, zOrderInfo);
                    }
                }
                catch { }
                
                throw;
            }

            // ログ出力
            var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var methodName = System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "Unknown";
            var resultMessage = $"Drag: {targetX1}, {targetY1} -> {targetX2}, {targetY2}";
            if (hwnd != IntPtr.Zero)
            {
                resultMessage += $" ({windowTitle}[{windowClassName}] {x1},{y1} -> {x2},{y2})";
            }
            GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);
        }

        private static void PerformMove(int x, int y, string windowTitle = "", string windowClassName = "") 
            => Cursor.SetPos(x, y, windowTitle, windowClassName);

        #endregion

        // 同期メソッド（後方互換性のため）
        public static void Click(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClickAsync(x, y, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName).Wait();

        public static void RightClick(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClickAsync(x, y, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName).Wait();

        public static void MiddleClick(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClickAsync(x, y, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName).Wait();

        public static void Wheel(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
            => PerformMouseActionAsync(x, y, MOUSEEVENTF_WHEEL, delta, windowTitle, windowClassName).Wait();

        public static void HWheel(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
            => PerformMouseActionAsync(x, y, MOUSEEVENTF_HWHEEL, delta, windowTitle, windowClassName).Wait();

        public static void Move(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMove(x, y, windowTitle, windowClassName);

        public static void Drag(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName).Wait();

        public static void RightDrag(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName).Wait();

        public static void MiddleDrag(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName).Wait();

        // 非同期メソッド（推奨）
        public static Task ClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClickAsync(x, y, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName);

        public static Task RightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClickAsync(x, y, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName);

        public static Task MiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
            => PerformMouseClickAsync(x, y, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName);

        public static Task WheelAsync(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
            => PerformMouseActionAsync(x, y, MOUSEEVENTF_WHEEL, delta, windowTitle, windowClassName);

        public static Task HWheelAsync(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
            => PerformMouseActionAsync(x, y, MOUSEEVENTF_HWHEEL, delta, windowTitle, windowClassName);

        public static Task DragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName);

        public static Task RightDragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName);

        public static Task MiddleDragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
            => PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName);
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

            if(windowTitle == "")
            {
                GetCursorPos(out lpPoint);
            }
            else
            {
                var hWnd = Window.GetHandle(windowTitle, windowClassName);
                var rect = Window.GetRect(hWnd);
                GetCursorPos(out lpPoint);
                lpPoint = new System.Drawing.Point(lpPoint.X - rect.Left, lpPoint.Y - rect.Top);
            }

            // ログ出力
            var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var methodName = System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "Unknown";
            var resultMessage = $"GetPos: {lpPoint.X}, {lpPoint.Y} ({windowTitle}[{windowClassName}])";
            GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);

            return lpPoint;
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
        private static volatile bool isLocked = false;
        private static readonly object lockObject = new object();

        public static void Lock(int x, int y)
        {
            lock (lockObject)
            {
                isLocked = true;
                
                Task.Run(async () =>
                {
                    while (isLocked)
                    {
                        SetPos(x, y);
                        await Task.Delay(10);
                    }
                });

                // ログ出力
                var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                var methodName = System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "Unknown";
                var resultMessage = $"Lock: {x}, {y}";
                GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);
            }
        }

        public static void Unlock()
        {
            lock (lockObject)
            {
                isLocked = false;

                // ログ出力
                var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                var methodName = System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "Unknown";
                var resultMessage = $"Unlock";
                GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);
            }
        }
        #endregion
    }

    /// <summary>
    /// ウィンドウのZオーダー情報
    /// </summary>
    public sealed class WindowZOrderInfo
    {
        public IntPtr WindowHandle { get; }
        public IntPtr NextWindow { get; }
        public IntPtr PrevWindow { get; }
        public IntPtr ForegroundWindow { get; }
        public DateTime CreatedAt { get; }

        public WindowZOrderInfo(IntPtr windowHandle, IntPtr nextWindow, IntPtr prevWindow, IntPtr foregroundWindow)
        {
            WindowHandle = windowHandle;
            NextWindow = nextWindow;
            PrevWindow = prevWindow;
            ForegroundWindow = foregroundWindow;
            CreatedAt = DateTime.Now;
        }

        public bool IsValid()
        {
            // 5秒以内の情報のみ有効とする
            return DateTime.Now - CreatedAt < TimeSpan.FromSeconds(5) && 
                   WindowHandle != IntPtr.Zero;
        }
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

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        private const uint GW_HWNDPREV = 3;
        private const uint GW_HWNDNEXT = 2;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOREDRAW = 0x0008;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOZORDER = 0x0004;

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
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), 
                    $"Window not found: '{windowTitle}' [{windowClassName}]");
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
        /// <summary>
        /// ウィンドウのZオーダー情報を保存
        /// </summary>
        public static WindowZOrderInfo SaveZOrder(IntPtr targetWindow)
        {
            if (!IsWindow(targetWindow))
                throw new ArgumentException("Invalid window handle", nameof(targetWindow));

            var nextWindow = GetWindow(targetWindow, GW_HWNDNEXT);
            var prevWindow = GetWindow(targetWindow, GW_HWNDPREV);
            var foregroundWindow = GetForegroundWindow();

            return new WindowZOrderInfo(targetWindow, nextWindow, prevWindow, foregroundWindow);
        }

        /// <summary>
        /// 特定ウィンドウをフォアグラウンドに設定
        /// </summary>
        public static void BringToFront(IntPtr targetWindow)
        {
            if (!IsWindow(targetWindow))
            {
                GlobalLogger.Instance.Write("MouseHelper", "BringToFront", "Invalid window handle");
                return;
            }

            try
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
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "BringToFront", $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// ウィンドウを元のZオーダーに復元
        /// </summary>
        public static void RestoreZOrder(IntPtr targetWindow, WindowZOrderInfo zOrderInfo)
        {
            if (!IsWindow(targetWindow) || !zOrderInfo.IsValid())
            {
                GlobalLogger.Instance.Write("MouseHelper", "RestoreZOrder", "Invalid window or expired Z-order info");
                return;
            }

            try
            {
                // 元のフォアグラウンドウィンドウを復元
                if (zOrderInfo.ForegroundWindow != IntPtr.Zero && 
                    IsWindow(zOrderInfo.ForegroundWindow) && 
                    zOrderInfo.ForegroundWindow != targetWindow)
                {
                    SetForegroundWindow(zOrderInfo.ForegroundWindow);
                }

                // Zオーダーを復元
                if (zOrderInfo.NextWindow != IntPtr.Zero && IsWindow(zOrderInfo.NextWindow))
                {
                    // 元の次のウィンドウの後に配置
                    SetWindowPos(targetWindow, zOrderInfo.NextWindow, 0, 0, 0, 0,
                                 SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
                else if (zOrderInfo.PrevWindow != IntPtr.Zero && IsWindow(zOrderInfo.PrevWindow))
                {
                    // 元の前のウィンドウの前に配置
                    SetWindowPos(zOrderInfo.PrevWindow, targetWindow, 0, 0, 0, 0,
                                 SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
                else
                {
                    // 前後の情報がない場合は最背面に配置
                    SetWindowPos(targetWindow, HWND_BOTTOM, 0, 0, 0, 0,
                                 SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "RestoreZOrder", $"Error: {ex.Message}");
            }
        }
        #endregion
    }
}
