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
    public static partial class Input // changed to partial
    {
        #region Win32API
        [DllImport("user32.dll", SetLastError = true)]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(System.Drawing.Point point);

        [DllImport("user32.dll")]
        static extern IntPtr ChildWindowFromPoint(IntPtr hWndParent, System.Drawing.Point point);

        [DllImport("user32.dll")]
        static extern IntPtr ChildWindowFromPointEx(IntPtr hwnd, System.Drawing.Point pt, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool IsWindowEnabled(IntPtr hWnd);

        // マウスイベントフラグ
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_HWHEEL = 0x01000;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        // ウィンドウメッセージ定数
        const uint WM_LBUTTONDOWN = 0x0201;
        const uint WM_LBUTTONUP = 0x0202;
        const uint WM_RBUTTONDOWN = 0x0204;
        const uint WM_RBUTTONUP = 0x0205;
        const uint WM_MBUTTONDOWN = 0x0207;
        const uint WM_MBUTTONUP = 0x0208;
        const uint WM_MOUSEWHEEL = 0x020A;
        const uint WM_MOUSEHWHEEL = 0x020E;
        const uint WM_MOUSEMOVE = 0x0200;

        // ChildWindowFromPointEx用フラグ
        const uint CWP_ALL = 0x0000;
        const uint CWP_SKIPINVISIBLE = 0x0001;
        const uint CWP_SKIPDISABLED = 0x0002;
        const uint CWP_SKIPTRANSPARENT = 0x0004;

        // マクロ関数
        static IntPtr MAKELPARAM(int loWord, int hiWord) => new IntPtr((hiWord << 16) | (loWord & 0xFFFF));
        static int GET_X_LPARAM(IntPtr lParam) => (int)(short)(lParam.ToInt32() & 0xFFFF);
        static int GET_Y_LPARAM(IntPtr lParam) => (int)(short)((lParam.ToInt32() >> 16) & 0xFFFF);

        // ゲーム向け追加API
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // ゲーム向け定数
        const uint WDA_NONE = 0x00000000;
        const uint WDA_MONITOR = 0x00000001;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_VM_READ = 0x0010;
        const uint PROCESS_VM_WRITE = 0x0020;

        // DirectInput座標系用
        const uint INPUT_MOUSE = 0;
        const uint INPUT_KEYBOARD = 1;
        const uint INPUT_HARDWARE = 2;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        #endregion

        #region Helper Methods

        /// <summary>
        /// 背景クリック方式の種類
        /// </summary>
        public enum BackgroundClickMethod
        {
            /// <summary>SendMessage使用（同期的）</summary>
            SendMessage,
            /// <summary>PostMessage使用（非同期的）</summary>
            PostMessage,
            /// <summary>子ウィンドウも含めて自動検出</summary>
            AutoDetectChild,
            /// <summary>複数の方式を試行</summary>
            TryAll,
            /// <summary>ゲーム向け：DirectInput座標系使用</summary>
            GameDirectInput,
            /// <summary>ゲーム向け：フルスクリーン対応</summary>
            GameFullscreen,
            /// <summary>ゲーム向け：低レベルAPI使用</summary>
            GameLowLevel,
            /// <summary>ゲーム向け：仮想マウス</summary>
            GameVirtualMouse
        }

        /// <summary>
        /// 対象ウィンドウハンドルを取得
        /// </summary>
        private static IntPtr GetTargetWindow(int x, int y, string windowTitle, string windowClassName)
        {
            if (!string.IsNullOrEmpty(windowTitle))
            {
                return Window.GetHandle(windowTitle, windowClassName);
            }
            else
            {
                var point = new System.Drawing.Point(x, y);
                IntPtr hwnd = WindowFromPoint(point);
                if (hwnd == IntPtr.Zero)
                {
                    throw new InvalidOperationException("指定座標にウィンドウが見つかりません");
                }
                return hwnd;
            }
        }

        /// <summary>
        /// クライアント座標に変換
        /// </summary>
        private static System.Drawing.Point ConvertToClientPoint(IntPtr hwnd, int x, int y, string windowTitle)
        {
            if (!string.IsNullOrEmpty(windowTitle))
            {
                // ウィンドウ相対座標の場合はそのまま使用
                return new System.Drawing.Point(x, y);
            }
            else
            {
                // グローバル座標の場合はクライアント座標に変換
                var point = new System.Drawing.Point(x, y);
                ScreenToClient(hwnd, ref point);
                return point;
            }
        }

        /// <summary>
        /// 背景クリックのログ出力
        /// </summary>
        private static void LogBackgroundClick(string method, int x, int y, string windowTitle, string windowClassName, IntPtr? targetHwnd = null)
        {
            var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var methodName = "LogBackgroundClick";
            var resultMessage = $"BackgroundClick[{method}]: {x}, {y}";
            
            if (!string.IsNullOrEmpty(windowTitle))
            {
                resultMessage += $" ({windowTitle}[{windowClassName}])";
            }
            
            if (targetHwnd.HasValue && targetHwnd != IntPtr.Zero)
            {
                resultMessage += $" Handle: 0x{targetHwnd.Value.ToInt64():X}";
            }
            
            GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);
        }

        #endregion

        #region Background Click Methods

        /// <summary>
        /// 方式1: SendMessage使用（基本的な背景クリック）
        /// </summary>
        private static async Task PerformSendMessageClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                IntPtr hwnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var clientPoint = ConvertToClientPoint(hwnd, x, y, windowTitle);

                IntPtr lParam = MAKELPARAM(clientPoint.X, clientPoint.Y);

                // SendMessageで同期的にメッセージ送信
                SendMessage(hwnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                SendMessage(hwnd, upMsg, IntPtr.Zero, lParam);

                LogBackgroundClick("SendMessage", clientPoint.X, clientPoint.Y, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformSendMessageClick", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 方式2: PostMessage使用（非同期的な背景クリック）
        /// </summary>
        private static async Task PerformPostMessageClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                IntPtr hwnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var clientPoint = ConvertToClientPoint(hwnd, x, y, windowTitle);

                IntPtr lParam = MAKELPARAM(clientPoint.X, clientPoint.Y);

                // PostMessageで非同期的にメッセージ送信
                PostMessage(hwnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                PostMessage(hwnd, upMsg, IntPtr.Zero, lParam);

                LogBackgroundClick("PostMessage", clientPoint.X, clientPoint.Y, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformPostMessageClick", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 方式3: 子ウィンドウ自動検出（より精密な背景クリック）
        /// </summary>
        private static async Task PerformChildWindowClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                IntPtr parentHwnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                
                // グローバル座標から親ウィンドウ相対座標に変換
                var parentPoint = ConvertToClientPoint(parentHwnd, x, y, windowTitle);
                
                // 子ウィンドウを検索（無効・非表示ウィンドウをスキップ）
                IntPtr childHwnd = ChildWindowFromPointEx(parentHwnd, parentPoint, CWP_SKIPINVISIBLE | CWP_SKIPDISABLED);
                
                IntPtr targetHwnd = childHwnd != IntPtr.Zero ? childHwnd : parentHwnd;
                System.Drawing.Point targetPoint;

                if (childHwnd != IntPtr.Zero && childHwnd != parentHwnd)
                {
                    // 子ウィンドウ相対座標に変換
                    var screenPoint = new System.Drawing.Point(x, y);
                    if (string.IsNullOrEmpty(windowTitle))
                    {
                        // 既にスクリーン座標の場合
                        targetPoint = screenPoint;
                    }
                    else
                    {
                        // ウィンドウ相対座標をスクリーン座標に変換してから子ウィンドウ相対座標に変換
                        var rect = Window.GetRect(parentHwnd);
                        screenPoint = new System.Drawing.Point(rect.Left + x, rect.Top + y);
                        targetPoint = screenPoint;
                    }
                    
                    ScreenToClient(targetHwnd, ref targetPoint);
                }
                else
                {
                    targetPoint = parentPoint;
                }

                IntPtr lParam = MAKELPARAM(targetPoint.X, targetPoint.Y);

                // 子ウィンドウまたは親ウィンドウにメッセージ送信
                SendMessage(targetHwnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                SendMessage(targetHwnd, upMsg, IntPtr.Zero, lParam);

                LogBackgroundClick("ChildWindow", targetPoint.X, targetPoint.Y, windowTitle, windowClassName, targetHwnd);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformChildWindowClick", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 方式4: 複数方式試行（最も確実な背景クリック）
        /// </summary>
        private static async Task PerformTryAllClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            var methods = new (string methodName, Func<Task> method)[]
            {
                // ゲーム向け方式を優先
                ("GameVirtualMouse", () => PerformGameVirtualMouseClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                ("GameDirectInput", () => PerformGameDirectInputClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                ("GameLowLevel", () => PerformGameLowLevelClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                ("GameFullscreen", () => PerformGameFullscreenClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                // 従来の方式
                ("ChildWindow", () => PerformChildWindowClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                ("SendMessage", () => PerformSendMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                ("PostMessage", () => PerformPostMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName))
            };

            Exception lastException = null;

            foreach (var (methodName, method) in methods)
            {
                try
                {
                    await method();
                    GlobalLogger.Instance.Write("MouseHelper", "PerformTryAllClick", $"成功: {methodName}");
                    return; // 成功した場合は終了
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    GlobalLogger.Instance.Write("MouseHelper", "PerformTryAllClick", $"失敗: {methodName} - {ex.Message}");
                    await Task.Delay(10); // 少し待機してから次の方式を試行
                }
            }

            // すべての方式が失敗した場合
            throw new Exception($"すべての背景クリック方式が失敗しました。最後のエラー: {lastException?.Message}");
        }

        /// <summary>
        /// 統合された背景クリック実行メソッド
        /// </summary>
        private static async Task PerformBackgroundClickAsync(int x, int y, uint downMsg, uint upMsg, 
            BackgroundClickMethod method = BackgroundClickMethod.SendMessage, 
            string windowTitle = "", string windowClassName = "")
        {
            switch (method)
            {
                case BackgroundClickMethod.SendMessage:
                    await PerformSendMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case BackgroundClickMethod.PostMessage:
                    await PerformPostMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case BackgroundClickMethod.AutoDetectChild:
                    await PerformChildWindowClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case BackgroundClickMethod.TryAll:
                    await PerformTryAllClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case BackgroundClickMethod.GameDirectInput:
                    await PerformGameDirectInputClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case BackgroundClickMethod.GameFullscreen:
                    await PerformGameFullscreenClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case BackgroundClickMethod.GameLowLevel:
                    await PerformGameLowLevelClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case BackgroundClickMethod.GameVirtualMouse:
                    await PerformGameVirtualMouseClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                default:
                    throw new ArgumentException($"未対応の背景クリック方式: {method}");
            }
        }

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

        #region Background Click Public Methods

        // 同期版背景クリックメソッド
        public static void BackgroundClick(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage)
            => PerformBackgroundClickAsync(x, y, WM_LBUTTONDOWN, WM_LBUTTONUP, method, windowTitle, windowClassName).Wait();

        public static void BackgroundRightClick(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage)
            => PerformBackgroundClickAsync(x, y, WM_RBUTTONDOWN, WM_RBUTTONUP, method, windowTitle, windowClassName).Wait();

        public static void BackgroundMiddleClick(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage)
            => PerformBackgroundClickAsync(x, y, WM_MBUTTONDOWN, WM_MBUTTONUP, method, windowTitle, windowClassName).Wait();

        // 非同期版背景クリックメソッド（推奨）
        public static Task BackgroundClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage)
            => PerformBackgroundClickAsync(x, y, WM_LBUTTONDOWN, WM_LBUTTONUP, method, windowTitle, windowClassName);

        public static Task BackgroundRightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage)
            => PerformBackgroundClickAsync(x, y, WM_RBUTTONDOWN, WM_RBUTTONUP, method, windowTitle, windowClassName);

        public static Task BackgroundMiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage)
            => PerformBackgroundClickAsync(x, y, WM_MBUTTONDOWN, WM_MBUTTONUP, method, windowTitle, windowClassName);

        #endregion // Background Click Public Methods
        #endregion // Action
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

    public static partial class Input // put game-specific methods inside same namespace & class
    {
        #region Game-specific Background Click Methods

        /// <summary>
        /// 方式5: ゲーム向けDirectInput座標系（相対座標使用）
        /// </summary>
        private static async Task PerformGameDirectInputClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                IntPtr hwnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var clientPoint = ConvertToClientPoint(hwnd, x, y, windowTitle);

                // ウィンドウ矩形から幅高さ取得
                var rectTmp = Window.GetRect(hwnd);
                int width = Math.Max(rectTmp.Right - rectTmp.Left, 1);
                int height = Math.Max(rectTmp.Bottom - rectTmp.Top, 1);

                var inputPoint = new System.Drawing.Point(
                    (int)((float)clientPoint.X / width * 65535f),
                    (int)((float)clientPoint.Y / height * 65535f));

                IntPtr lParam = MAKELPARAM(inputPoint.X, inputPoint.Y);

                SendMessage(hwnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                SendMessage(hwnd, upMsg, IntPtr.Zero, lParam);

                LogBackgroundClick("GameDirectInput", clientPoint.X, clientPoint.Y, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformGameDirectInputClick", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 方式6: ゲーム向けフルスクリーン対応（特別な処理なし）
        /// </summary>
        private static async Task PerformGameFullscreenClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                IntPtr hwnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var clientPoint = ConvertToClientPoint(hwnd, x, y, windowTitle);

                IntPtr lParam = MAKELPARAM(clientPoint.X, clientPoint.Y);

                // フルスクリーンアプリにメッセージ送信
                SendMessage(hwnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                SendMessage(hwnd, upMsg, IntPtr.Zero, lParam);

                LogBackgroundClick("GameFullscreen", clientPoint.X, clientPoint.Y, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformGameFullscreenClick", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 方式7: ゲーム向け低レベルAPI使用（特権昇格が必要）
        /// </summary>
        private static async Task PerformGameLowLevelClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                IntPtr hwnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var clientPoint = ConvertToClientPoint(hwnd, x, y, windowTitle);

                // 低レベルAPI用に座標を調整
                var lowLevelPoint = new System.Drawing.Point(clientPoint.X, clientPoint.Y);

                IntPtr lParam = MAKELPARAM(lowLevelPoint.X, lowLevelPoint.Y);

                // ゲームウィンドウにメッセージ送信
                SendMessage(hwnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                SendMessage(hwnd, upMsg, IntPtr.Zero, lParam);

                LogBackgroundClick("GameLowLevel", clientPoint.X, clientPoint.Y, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformGameLowLevelClick", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 方式8: ゲーム向け仮想マウス使用（特別な処理なし）
        /// </summary>
        private static async Task PerformGameVirtualMouseClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                // 仮想マウス用の処理
                Cursor.SetPos(x, y, windowTitle, windowClassName);
                await Task.Delay(30);
                mouse_event(downMsg, 0, 0, 0, 0);
                await Task.Delay(30);
                mouse_event(upMsg, 0, 0, 0, 0);

                LogBackgroundClick("GameVirtualMouse", x, y, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.Write("MouseHelper", "PerformGameVirtualMouseClick", $"Error: {ex.Message}");
                throw;
            }
        }
        #endregion
    }
}
