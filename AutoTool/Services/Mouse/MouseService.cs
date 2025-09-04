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
    /// �}�E�X����T�[�r�X�̎���
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
        /// ���݂̃}�E�X�ʒu���擾�i�X�N���[�����W�j
        /// </summary>
        public Point GetCurrentPosition()
        {
            try
            {
                if (GetCursorPos(out Point point))
                {
                    _logger.LogDebug("�}�E�X�ʒu�擾����: ({X}, {Y})", point.X, point.Y);
                    return point;
                }
                else
                {
                    _logger.LogWarning("�}�E�X�ʒu�̎擾�Ɏ��s���܂���");
                    return Point.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�E�X�ʒu�擾���ɃG���[");
                return Point.Empty;
            }
        }

        /// <summary>
        /// �w�肳�ꂽ�E�B���h�E�ł̃N���C�A���g���W���擾
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

                // �E�B���h�E�n���h�����擾
                var hWnd = FindWindow(windowClassName, windowTitle);
                if (hWnd == IntPtr.Zero)
                {
                    _logger.LogWarning("�E�B���h�E��������܂���: Title={WindowTitle}, ClassName={WindowClassName}", 
                        windowTitle, windowClassName ?? "null");
                    return screenPos; // �E�B���h�E��������Ȃ��ꍇ�̓X�N���[�����W��Ԃ�
                }

                // �E�B���h�E���L�����m�F
                if (!IsWindow(hWnd) || !IsWindowVisible(hWnd))
                {
                    _logger.LogWarning("�E�B���h�E�������܂��͔�\��: Title={WindowTitle}", windowTitle);
                    return screenPos;
                }

                // �X�N���[�����W���N���C�A���g���W�ɕϊ�
                var clientPos = screenPos;
                if (ScreenToClient(hWnd, ref clientPos))
                {
                    _logger.LogDebug("���W�ϊ�����: Screen({SX}, {SY}) -> Client({CX}, {CY})", 
                        screenPos.X, screenPos.Y, clientPos.X, clientPos.Y);
                    return clientPos;
                }
                else
                {
                    _logger.LogWarning("���W�ϊ��Ɏ��s: Screen({SX}, {SY})", screenPos.X, screenPos.Y);
                    return screenPos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�N���C�A���g���W�擾���ɃG���[: WindowTitle={WindowTitle}", windowTitle);
                return GetCurrentPosition(); // �t�H�[���o�b�N
            }
        }

        /// <summary>
        /// �E�N���b�N�ҋ@���[�h���J�n�i�񓯊��j
        /// </summary>
        public async Task<Point> WaitForRightClickAsync(string? windowTitle = null, string? windowClassName = null)
        {
            try
            {
                _logger.LogInformation("�E�N���b�N�ҋ@�J�n: WindowTitle={WindowTitle}, ClassName={ClassName}", 
                    windowTitle ?? "null", windowClassName ?? "null");

                // �����̑ҋ@���L�����Z��
                CancelRightClickWait();

                // �V�����ҋ@���J�n
                _cancellationTokenSource = new CancellationTokenSource();
                _rightClickTcs = new TaskCompletionSource<Point>();

                // �}�E�X�t�b�N��ݒ�
                _mouseHookProc = (nCode, wParam, lParam) => MouseHookProc(nCode, wParam, lParam, windowTitle, windowClassName);
                _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, GetModuleHandle("user32"), 0);

                if (_mouseHookId == IntPtr.Zero)
                {
                    throw new InvalidOperationException("�}�E�X�t�b�N�̐ݒ�Ɏ��s���܂���");
                }

                // �^�C���A�E�g�����i30�b�j
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, timeoutCts.Token);

                combinedCts.Token.Register(() =>
                {
                    if (!_rightClickTcs.Task.IsCompleted)
                    {
                        if (timeoutCts.Token.IsCancellationRequested)
                        {
                            _rightClickTcs.TrySetException(new TimeoutException("�E�N���b�N�ҋ@���^�C���A�E�g���܂���"));
                        }
                        else
                        {
                            _rightClickTcs.TrySetCanceled();
                        }
                    }
                });

                var result = await _rightClickTcs.Task;
                _logger.LogInformation("�E�N���b�N���o����: ({X}, {Y})", result.X, result.Y);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("�E�N���b�N�ҋ@���L�����Z������܂���");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�N���b�N�ҋ@���ɃG���[");
                throw;
            }
            finally
            {
                CleanupHook();
            }
        }

        /// <summary>
        /// �E�N���b�N�ҋ@���L�����Z��
        /// </summary>
        public void CancelRightClickWait()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                CleanupHook();
                _logger.LogDebug("�E�N���b�N�ҋ@���L�����Z�����܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�N���b�N�ҋ@�L�����Z�����ɃG���[");
            }
        }

        /// <summary>
        /// �}�E�X�t�b�N�v���V�[�W��
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
                        // �w�肳�ꂽ�E�B���h�E�̃N���C�A���g���W�ɕϊ�
                        var hWnd = FindWindow(targetWindowClassName, targetWindowTitle);
                        if (hWnd != IntPtr.Zero && IsWindow(hWnd) && IsWindowVisible(hWnd))
                        {
                            var clientPos = screenPos;
                            if (ScreenToClient(hWnd, ref clientPos))
                            {
                                resultPos = clientPos;
                                _logger.LogDebug("�E�N���b�N���o�i�N���C�A���g���W�j: ({X}, {Y})", clientPos.X, clientPos.Y);
                            }
                            else
                            {
                                resultPos = screenPos;
                                _logger.LogDebug("�E�N���b�N���o�i�X�N���[�����W�E�ϊ����s�j: ({X}, {Y})", screenPos.X, screenPos.Y);
                            }
                        }
                        else
                        {
                            resultPos = screenPos;
                            _logger.LogDebug("�E�N���b�N���o�i�X�N���[�����W�E�E�B���h�E�����j: ({X}, {Y})", screenPos.X, screenPos.Y);
                        }
                    }
                    else
                    {
                        resultPos = screenPos;
                        _logger.LogDebug("�E�N���b�N���o�i�X�N���[�����W�j: ({X}, {Y})", screenPos.X, screenPos.Y);
                    }

                    // �E�N���b�N�����o�����̂Ō��ʂ�ݒ�
                    _rightClickTcs?.TrySetResult(resultPos);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�E�X�t�b�N�������ɃG���[");
                _rightClickTcs?.TrySetException(ex);
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// �t�b�N�̃N���[���A�b�v
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
                _logger.LogError(ex, "�t�b�N�N���[���A�b�v���ɃG���[");
            }
        }

        /// <summary>
        /// ���\�[�X�̉��
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
                _logger.LogError(ex, "MouseService dispose���ɃG���[");
            }
        }
    }
}