using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Mouse
{
    /// <summary>
    /// マウス操作サービスの実装
    /// </summary>
    public class MouseService : IMouseService, IDisposable
    {
        private readonly ILogger<MouseService> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private TaskCompletionSource<Point>? _rightClickTcs;
        private LowLevelMouseProc? _mouseHookProc;
        private IntPtr _mouseHookId = IntPtr.Zero;

        public MouseService(ILogger<MouseService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_RBUTTONDOWN = 0x0204;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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

        #endregion

        /// <summary>
        /// 現在のマウス位置を取得（スクリーン座標）
        /// </summary>
        public Point GetCurrentPosition()
        {
            try
            {
                if (GetCursorPos(out Point point))
                {
                    _logger.LogDebug("マウス位置取得成功: ({X}, {Y})", point.X, point.Y);
                    return point;
                }
                else
                {
                    _logger.LogWarning("マウス位置の取得に失敗しました");
                    return Point.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置取得中にエラー");
                return Point.Empty;
            }
        }

        /// <summary>
        /// 指定されたウィンドウでのクライアント座標を取得
        /// </summary>
        public Point GetClientPosition(string windowTitle, string? windowClassName = null)
        {
            try
            {
                var screenPos = GetCurrentPosition();
                if (screenPos == Point.Empty)
                {
                    return Point.Empty;
                }

                // ウィンドウハンドルを取得
                var hWnd = FindWindow(windowClassName, windowTitle);
                if (hWnd == IntPtr.Zero)
                {
                    _logger.LogWarning("ウィンドウが見つかりません: Title={WindowTitle}, ClassName={WindowClassName}", 
                        windowTitle, windowClassName ?? "null");
                    return screenPos; // ウィンドウが見つからない場合はスクリーン座標を返す
                }

                // ウィンドウが有効か確認
                if (!IsWindow(hWnd) || !IsWindowVisible(hWnd))
                {
                    _logger.LogWarning("ウィンドウが無効または非表示: Title={WindowTitle}", windowTitle);
                    return screenPos;
                }

                // スクリーン座標をクライアント座標に変換
                var clientPos = screenPos;
                if (ScreenToClient(hWnd, ref clientPos))
                {
                    _logger.LogDebug("座標変換成功: Screen({SX}, {SY}) -> Client({CX}, {CY})", 
                        screenPos.X, screenPos.Y, clientPos.X, clientPos.Y);
                    return clientPos;
                }
                else
                {
                    _logger.LogWarning("座標変換に失敗: Screen({SX}, {SY})", screenPos.X, screenPos.Y);
                    return screenPos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クライアント座標取得中にエラー: WindowTitle={WindowTitle}", windowTitle);
                return GetCurrentPosition(); // フォールバック
            }
        }

        /// <summary>
        /// 右クリック待機モードを開始（非同期）
        /// </summary>
        public async Task<Point> WaitForRightClickAsync(string? windowTitle = null, string? windowClassName = null)
        {
            try
            {
                _logger.LogInformation("右クリック待機開始: WindowTitle={WindowTitle}, ClassName={ClassName}", 
                    windowTitle ?? "null", windowClassName ?? "null");

                // 既存の待機をキャンセル
                CancelRightClickWait();

                // 新しい待機を開始
                _cancellationTokenSource = new CancellationTokenSource();
                _rightClickTcs = new TaskCompletionSource<Point>();

                // マウスフックを設定
                _mouseHookProc = (nCode, wParam, lParam) => MouseHookProc(nCode, wParam, lParam, windowTitle, windowClassName);
                _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, GetModuleHandle("user32"), 0);

                if (_mouseHookId == IntPtr.Zero)
                {
                    throw new InvalidOperationException("マウスフックの設定に失敗しました");
                }

                // タイムアウト処理（30秒）
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, timeoutCts.Token);

                combinedCts.Token.Register(() =>
                {
                    if (!_rightClickTcs.Task.IsCompleted)
                    {
                        if (timeoutCts.Token.IsCancellationRequested)
                        {
                            _rightClickTcs.TrySetException(new TimeoutException("右クリック待機がタイムアウトしました"));
                        }
                        else
                        {
                            _rightClickTcs.TrySetCanceled();
                        }
                    }
                });

                var result = await _rightClickTcs.Task;
                _logger.LogInformation("右クリック検出成功: ({X}, {Y})", result.X, result.Y);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("右クリック待機がキャンセルされました");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリック待機中にエラー");
                throw;
            }
            finally
            {
                CleanupHook();
            }
        }

        /// <summary>
        /// 右クリック待機をキャンセル
        /// </summary>
        public void CancelRightClickWait()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                CleanupHook();
                _logger.LogDebug("右クリック待機をキャンセルしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリック待機キャンセル中にエラー");
            }
        }

        /// <summary>
        /// マウスフックプロシージャ
        /// </summary>
        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam, string? targetWindowTitle, string? targetWindowClassName)
        {
            try
            {
                if (nCode >= 0 && wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var screenPos = new Point(hookStruct.pt.x, hookStruct.pt.y);

                    Point resultPos;
                    if (!string.IsNullOrEmpty(targetWindowTitle))
                    {
                        // 指定されたウィンドウのクライアント座標に変換
                        var hWnd = FindWindow(targetWindowClassName, targetWindowTitle);
                        if (hWnd != IntPtr.Zero && IsWindow(hWnd) && IsWindowVisible(hWnd))
                        {
                            var clientPos = screenPos;
                            if (ScreenToClient(hWnd, ref clientPos))
                            {
                                resultPos = clientPos;
                                _logger.LogDebug("右クリック検出（クライアント座標）: ({X}, {Y})", clientPos.X, clientPos.Y);
                            }
                            else
                            {
                                resultPos = screenPos;
                                _logger.LogDebug("右クリック検出（スクリーン座標・変換失敗）: ({X}, {Y})", screenPos.X, screenPos.Y);
                            }
                        }
                        else
                        {
                            resultPos = screenPos;
                            _logger.LogDebug("右クリック検出（スクリーン座標・ウィンドウ無効）: ({X}, {Y})", screenPos.X, screenPos.Y);
                        }
                    }
                    else
                    {
                        resultPos = screenPos;
                        _logger.LogDebug("右クリック検出（スクリーン座標）: ({X}, {Y})", screenPos.X, screenPos.Y);
                    }

                    // 右クリックを検出したので結果を設定
                    _rightClickTcs?.TrySetResult(resultPos);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウスフック処理中にエラー");
                _rightClickTcs?.TrySetException(ex);
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// フックのクリーンアップ
        /// </summary>
        private void CleanupHook()
        {
            try
            {
                if (_mouseHookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHookId);
                    _mouseHookId = IntPtr.Zero;
                }
                _mouseHookProc = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "フッククリーンアップ中にエラー");
            }
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            try
            {
                CancelRightClickWait();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MouseService dispose中にエラー");
            }
        }
    }
}