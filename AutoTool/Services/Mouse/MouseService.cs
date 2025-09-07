using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Mouse
{
    /// <summary>
    /// マウス操作サービスの実装
    /// </summary>
    public class MouseService : IMouseService, IDisposable
    {
        private readonly ILogger<MouseService> _logger;
        private volatile bool _isLocked = false;
        private readonly object _lockObject = new object();
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelMouseProc? _mouseProc;
        private TaskCompletionSource<Point>? _rightClickTcs;
        private CancellationTokenSource? _cancellationTokenSource;

        public MouseService(ILogger<MouseService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mouseProc = HookCallback;
        }

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr ChildWindowFromPointEx(IntPtr hwnd, Point pt, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // フックデリゲート
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        // 構造体
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

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // 定数
        private const int WH_MOUSE_LL = 14;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x01000;
        private const uint MOUSEEVENTF_MOVE = 0x0001;

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP = 0x0208;
        private const uint WM_MOUSEWHEEL = 0x020A;
        private const uint WM_MOUSEHWHEEL = 0x020E;

        private const uint CWP_SKIPINVISIBLE = 0x0001;
        private const uint CWP_SKIPDISABLED = 0x0002;

        // ヘルパーメソッド
        private static IntPtr MAKELPARAM(int loWord, int hiWord) => new IntPtr((hiWord << 16) | (loWord & 0xFFFF));

        #endregion

        #region 既存メソッド（後方互換性）

        /// <summary>
        /// 現在のマウス位置を取得（スクリーン座標）
        /// </summary>
        public Point GetCurrentPosition()
        {
            return GetCurrentMousePosition();
        }

        /// <summary>
        /// 指定されたウィンドウでのクライアント座標を取得
        /// </summary>
        public Point GetClientPosition(string windowTitle, string? windowClassName = null)
        {
            return GetCurrentMousePosition(windowTitle, windowClassName ?? "");
        }

        /// <summary>
        /// 右クリック待機モードを開始（非同期）
        /// </summary>
        public async Task<Point> WaitForRightClickAsync(string? windowTitle = null, string? windowClassName = null)
        {
            var result = await WaitForRightClickWithTimeoutAsync(TimeSpan.FromMinutes(1));
            return result ?? new Point(0, 0);
        }

        /// <summary>
        /// 右クリック待機をキャンセル
        /// </summary>
        public void CancelRightClickWait()
        {
            StopMouseHook();
            _rightClickTcs?.TrySetCanceled();
        }

        #endregion

        #region 新しいメソッド（MouseHelper移植）

        /// <summary>
        /// 現在のマウス位置を取得
        /// </summary>
        public Point GetCurrentMousePosition()
        {
            try
            {
                GetCursorPos(out Point point);
                return point;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置取得エラー");
                return new Point(0, 0);
            }
        }

        /// <summary>
        /// 指定ウィンドウ相対でのマウス位置を取得
        /// </summary>
        public Point GetCurrentMousePosition(string windowTitle, string windowClassName = "")
        {
            try
            {
                var point = GetCurrentMousePosition();

                if (!string.IsNullOrEmpty(windowTitle))
                {
                    var hWnd = GetWindowHandle(windowTitle, windowClassName);
                    var rect = GetWindowRect(hWnd);
                    point = new Point(point.X - rect.Left, point.Y - rect.Top);
                }

                return point;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ相対マウス位置取得エラー");
                return new Point(0, 0);
            }
        }

        /// <summary>
        /// マウス位置を設定
        /// </summary>
        public void SetMousePosition(int x, int y, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(windowTitle))
                {
                    SetCursorPos(x, y);
                }
                else
                {
                    var hWnd = GetWindowHandle(windowTitle, windowClassName);
                    var rect = GetWindowRect(hWnd);
                    SetCursorPos(rect.Left + x, rect.Top + y);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置設定エラー");
            }
        }

        /// <summary>
        /// 左クリック
        /// </summary>
        public async Task ClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
        {
            await PerformMouseClickAsync(x, y, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName);
        }

        /// <summary>
        /// 右クリック
        /// </summary>
        public async Task RightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
        {
            await PerformMouseClickAsync(x, y, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName);
        }

        /// <summary>
        /// 中クリック
        /// </summary>
        public async Task MiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "")
        {
            await PerformMouseClickAsync(x, y, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName);
        }

        /// <summary>
        /// ドラッグ操作
        /// </summary>
        public async Task DragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
        {
            await PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName);
        }

        /// <summary>
        /// 右ドラッグ操作
        /// </summary>
        public async Task RightDragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
        {
            await PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName);
        }

        /// <summary>
        /// 中ドラッグ操作
        /// </summary>
        public async Task MiddleDragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "")
        {
            await PerformDragAsync(x1, y1, x2, y2, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName);
        }

        /// <summary>
        /// ホイール操作
        /// </summary>
        public async Task WheelAsync(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
        {
            await PerformMouseActionAsync(x, y, MOUSEEVENTF_WHEEL, delta, windowTitle, windowClassName);
        }

        /// <summary>
        /// 水平ホイール操作
        /// </summary>
        public async Task HWheelAsync(int x, int y, int delta, string windowTitle = "", string windowClassName = "")
        {
            await PerformMouseActionAsync(x, y, MOUSEEVENTF_HWHEEL, delta, windowTitle, windowClassName);
        }

        /// <summary>
        /// 背景左クリック
        /// </summary>
        public async Task BackgroundClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", IMouseService.BackgroundClickMethod method = IMouseService.BackgroundClickMethod.AutoDetectChild)
        {
            await PerformBackgroundClickAsync(x, y, WM_LBUTTONDOWN, WM_LBUTTONUP, method, windowTitle, windowClassName);
        }

        /// <summary>
        /// 背景右クリック
        /// </summary>
        public async Task BackgroundRightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", IMouseService.BackgroundClickMethod method = IMouseService.BackgroundClickMethod.AutoDetectChild)
        {
            await PerformBackgroundClickAsync(x, y, WM_RBUTTONDOWN, WM_RBUTTONUP, method, windowTitle, windowClassName);
        }

        /// <summary>
        /// 背景中クリック
        /// </summary>
        public async Task BackgroundMiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", IMouseService.BackgroundClickMethod method = IMouseService.BackgroundClickMethod.AutoDetectChild)
        {
            await PerformBackgroundClickAsync(x, y, WM_MBUTTONDOWN, WM_MBUTTONUP, method, windowTitle, windowClassName);
        }

        /// <summary>
        /// 右クリック待機（タイムアウト付き）
        /// </summary>
        public async Task<Point?> WaitForRightClickWithTimeoutAsync(TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromMinutes(1);

            try
            {
                _rightClickTcs = new TaskCompletionSource<Point>();
                StartMouseHook();

                var timeoutTask = Task.Delay(timeout);
                var completedTask = await Task.WhenAny(_rightClickTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("右クリック待機がタイムアウトしました");
                    return null;
                }

                return await _rightClickTcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリック待機エラー");
                return null;
            }
            finally
            {
                StopMouseHook();
                _rightClickTcs = null;
            }
        }

        /// <summary>
        /// マウスフック開始
        /// </summary>
        public void StartMouseHook()
        {
            if (_hookId == IntPtr.Zero && _mouseProc != null)
            {
                _hookId = SetHook(_mouseProc);
            }
        }

        /// <summary>
        /// マウスフック停止
        /// </summary>
        public void StopMouseHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// カーソルロック
        /// </summary>
        public void LockCursor(int x, int y)
        {
            lock (_lockObject)
            {
                _isLocked = true;

                Task.Run(async () =>
                {
                    while (_isLocked)
                    {
                        SetCursorPos(x, y);
                        await Task.Delay(10);
                    }
                });

                _logger.LogDebug("カーソルロック開始: {X}, {Y}", x, y);
            }
        }

        /// <summary>
        /// カーソルロック解除
        /// </summary>
        public void UnlockCursor()
        {
            lock (_lockObject)
            {
                _isLocked = false;
                _logger.LogDebug("カーソルロック解除");
            }
        }

        /// <summary>
        /// 指定座標の色を取得
        /// </summary>
        public Color GetColorAt(Point position)
        {
            try
            {
                using (var bitmap = new Bitmap(1, 1))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(position.X, position.Y, 0, 0, new Size(1, 1));
                    }
                    return bitmap.GetPixel(0, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色取得エラー: {Position}", position);
                return Color.Black;
            }
        }

        #endregion

        #region Private Methods

        private async Task PerformMouseClickAsync(int x, int y, uint downEvent, uint upEvent, string windowTitle, string windowClassName)
        {
            var targetX = x;
            var targetY = y;
            var originalPos = GetCurrentMousePosition();

            try
            {
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    var hWnd = GetWindowHandle(windowTitle, windowClassName);
                    var rect = GetWindowRect(hWnd);
                    targetX += rect.Left;
                    targetY += rect.Top;
                }

                LockCursor(targetX, targetY);
                await Task.Delay(100);

                mouse_event(downEvent, 0, 0, 0, 0);
                await Task.Delay(30);
                mouse_event(upEvent, 0, 0, 0, 0);
                await Task.Delay(30);

                UnlockCursor();
                SetCursorPos(originalPos.X, originalPos.Y);

                _logger.LogDebug("マウスクリック実行: {X}, {Y}", targetX, targetY);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウスクリック実行エラー");
                UnlockCursor();
                SetCursorPos(originalPos.X, originalPos.Y);
                throw;
            }
        }

        private async Task PerformDragAsync(int x1, int y1, int x2, int y2, uint downEvent, uint upEvent, string windowTitle, string windowClassName)
        {
            var targetX1 = x1;
            var targetY1 = y1;
            var targetX2 = x2;
            var targetY2 = y2;
            var originalPos = GetCurrentMousePosition();

            try
            {
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    var hWnd = GetWindowHandle(windowTitle, windowClassName);
                    var rect = GetWindowRect(hWnd);
                    targetX1 += rect.Left;
                    targetY1 += rect.Top;
                    targetX2 += rect.Left;
                    targetY2 += rect.Top;
                }

                LockCursor(targetX1, targetY1);
                mouse_event(downEvent, 0, 0, 0, 0);
                UnlockCursor();
                await Task.Delay(30);

                LockCursor(targetX2, targetY2);
                mouse_event(upEvent, 0, 0, 0, 0);
                UnlockCursor();

                SetCursorPos(originalPos.X, originalPos.Y);

                _logger.LogDebug("ドラッグ実行: {X1}, {Y1} -> {X2}, {Y2}", targetX1, targetY1, targetX2, targetY2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ドラッグ実行エラー");
                UnlockCursor();
                SetCursorPos(originalPos.X, originalPos.Y);
                throw;
            }
        }

        private async Task PerformMouseActionAsync(int x, int y, uint actionEvent, int delta, string windowTitle, string windowClassName)
        {
            var targetX = x;
            var targetY = y;
            var originalPos = GetCurrentMousePosition();

            try
            {
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    var hWnd = GetWindowHandle(windowTitle, windowClassName);
                    var rect = GetWindowRect(hWnd);
                    targetX += rect.Left;
                    targetY += rect.Top;
                }

                LockCursor(targetX, targetY);
                mouse_event(actionEvent, 0, 0, (uint)delta, 0);
                UnlockCursor();

                SetCursorPos(originalPos.X, originalPos.Y);

                _logger.LogDebug("マウスアクション実行: {X}, {Y}, Delta: {Delta}", targetX, targetY, delta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウスアクション実行エラー");
                UnlockCursor();
                SetCursorPos(originalPos.X, originalPos.Y);
                throw;
            }
        }

        private async Task PerformBackgroundClickAsync(int x, int y, uint downMsg, uint upMsg, IMouseService.BackgroundClickMethod method, string windowTitle, string windowClassName)
        {
            switch (method)
            {
                case IMouseService.BackgroundClickMethod.SendMessage:
                    await PerformSendMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case IMouseService.BackgroundClickMethod.PostMessage:
                    await PerformPostMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case IMouseService.BackgroundClickMethod.AutoDetectChild:
                    await PerformChildWindowClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                case IMouseService.BackgroundClickMethod.TryAll:
                    await PerformTryAllClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
                default:
                    await PerformSendMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName);
                    break;
            }
        }

        private async Task PerformSendMessageClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle, string windowClassName)
        {
            try
            {
                var hWnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var clientPoint = ConvertToClientPoint(hWnd, x, y, windowTitle);

                IntPtr lParam = MAKELPARAM(clientPoint.X, clientPoint.Y);

                SendMessage(hWnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                SendMessage(hWnd, upMsg, IntPtr.Zero, lParam);

                _logger.LogDebug("SendMessage背景クリック実行: {X}, {Y}", clientPoint.X, clientPoint.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessage背景クリックエラー");
                throw;
            }
        }

        private async Task PerformPostMessageClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle, string windowClassName)
        {
            try
            {
                var hWnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var clientPoint = ConvertToClientPoint(hWnd, x, y, windowTitle);

                IntPtr lParam = MAKELPARAM(clientPoint.X, clientPoint.Y);

                PostMessage(hWnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                PostMessage(hWnd, upMsg, IntPtr.Zero, lParam);

                _logger.LogDebug("PostMessage背景クリック実行: {X}, {Y}", clientPoint.X, clientPoint.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostMessage背景クリックエラー");
                throw;
            }
        }

        private async Task PerformChildWindowClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle, string windowClassName)
        {
            try
            {
                var parentHwnd = GetTargetWindow(x, y, windowTitle, windowClassName);
                var parentPoint = ConvertToClientPoint(parentHwnd, x, y, windowTitle);

                var childHwnd = ChildWindowFromPointEx(parentHwnd, parentPoint, CWP_SKIPINVISIBLE | CWP_SKIPDISABLED);
                var targetHwnd = childHwnd != IntPtr.Zero ? childHwnd : parentHwnd;

                Point targetPoint = parentPoint;
                if (childHwnd != IntPtr.Zero && childHwnd != parentHwnd)
                {
                    var screenPoint = new Point(x, y);
                    if (string.IsNullOrEmpty(windowTitle))
                    {
                        targetPoint = screenPoint;
                    }
                    else
                    {
                        var rect = GetWindowRect(parentHwnd);
                        screenPoint = new Point(rect.Left + x, rect.Top + y);
                        targetPoint = screenPoint;
                    }
                    ScreenToClient(targetHwnd, ref targetPoint);
                }

                IntPtr lParam = MAKELPARAM(targetPoint.X, targetPoint.Y);

                SendMessage(targetHwnd, downMsg, IntPtr.Zero, lParam);
                await Task.Delay(30);
                SendMessage(targetHwnd, upMsg, IntPtr.Zero, lParam);

                _logger.LogDebug("子ウィンドウ背景クリック実行: {X}, {Y}", targetPoint.X, targetPoint.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "子ウィンドウ背景クリックエラー");
                throw;
            }
        }

        private async Task PerformTryAllClickAsync(int x, int y, uint downMsg, uint upMsg, string windowTitle, string windowClassName)
        {
            var methods = new (string methodName, Func<Task> method)[]
            {
                ("ChildWindow", () => PerformChildWindowClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                ("SendMessage", () => PerformSendMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName)),
                ("PostMessage", () => PerformPostMessageClickAsync(x, y, downMsg, upMsg, windowTitle, windowClassName))
            };

            Exception? lastException = null;

            foreach (var (methodName, method) in methods)
            {
                try
                {
                    await method();
                    _logger.LogDebug("背景クリック成功: {MethodName}", methodName);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogDebug(ex, "背景クリック失敗: {MethodName}", methodName);
                    await Task.Delay(10);
                }
            }

            throw new Exception($"すべての背景クリック方式が失敗しました。最後のエラー: {lastException?.Message}");
        }

        private IntPtr GetTargetWindow(int x, int y, string windowTitle, string windowClassName)
        {
            if (!string.IsNullOrEmpty(windowTitle))
            {
                return GetWindowHandle(windowTitle, windowClassName);
            }
            else
            {
                var point = new Point(x, y);
                IntPtr hwnd = WindowFromPoint(point);
                if (hwnd == IntPtr.Zero)
                {
                    throw new InvalidOperationException("指定座標にウィンドウが見つかりません");
                }
                return hwnd;
            }
        }

        private Point ConvertToClientPoint(IntPtr hWnd, int x, int y, string windowTitle)
        {
            if (!string.IsNullOrEmpty(windowTitle))
            {
                return new Point(x, y);
            }
            else
            {
                var point = new Point(x, y);
                ScreenToClient(hWnd, ref point);
                return point;
            }
        }

        private IntPtr GetWindowHandle(string windowTitle, string windowClassName)
        {
            var hWnd = FindWindow(string.IsNullOrEmpty(windowClassName) ? null : windowClassName, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new InvalidOperationException($"ウィンドウが見つかりません: '{windowTitle}' [{windowClassName}]");
            }
            return hWnd;
        }

        private RECT GetWindowRect(IntPtr hWnd)
        {
            if (!GetWindowRect(hWnd, out RECT rect))
            {
                throw new InvalidOperationException("ウィンドウ矩形の取得に失敗しました");
            }
            return rect;
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                if (curProcess?.MainModule == null)
                {
                    return IntPtr.Zero;
                }

                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (int)wParam == (int)WM_RBUTTONDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var point = new Point(hookStruct.pt.x, hookStruct.pt.y);
                
                _rightClickTcs?.TrySetResult(point);
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopMouseHook();
            UnlockCursor();
            _rightClickTcs?.TrySetCanceled();
            _cancellationTokenSource?.Dispose();
        }

        #endregion
    }
}