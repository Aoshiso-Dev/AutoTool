using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Window
{
    public interface IWindowInfoService
    {
        /// <summary>
        /// ���[�U�[�Ɏ擾�������E�B���h�E��ŉE�N���b�N���Ă��炢�iEsc �ŃL�����Z���j�A���̃N���b�N�����ɂ���E�B���h�E�̃^�C�g���ƃN���X�����擾���܂��B
        /// �A�N�e�B�u���͕s�v�ł��B
        /// </summary>
        Task<(string Title, string ClassName)> WaitForActiveWindowSelectionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Win32 API ��p�����E�B���h�E���擾�T�[�r�X
    /// </summary>
    public class WindowInfoService : IWindowInfoService
    {
        private readonly ILogger<WindowInfoService> _logger;

        public WindowInfoService(ILogger<WindowInfoService> logger)
        {
            _logger = logger;
        }

        public async Task<(string Title, string ClassName)> WaitForActiveWindowSelectionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("WindowInfoService: �E�N���b�N�ʒu�����̃E�B���h�E�I��ҋ@�J�n (�E�N���b�N�Ŋm�� / Esc�ŃL�����Z��)");
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsKeyOrButtonPressed(VK_ESCAPE))
                {
                    _logger.LogInformation("WindowInfoService: Esc �L�����Z��");
                    throw new OperationCanceledException();
                }
                if (IsKeyOrButtonPressed(VK_RBUTTON))
                {
                    if (!GetCursorPos(out POINT pt))
                    {
                        _logger.LogWarning("WindowInfoService: GetCursorPos ���s");
                        return (string.Empty, string.Empty);
                    }

                    var hWnd = WindowFromPoint(pt);
                    if (hWnd == IntPtr.Zero)
                    {
                        _logger.LogWarning("WindowInfoService: WindowFromPoint �Ŏ擾���s");
                        return (string.Empty, string.Empty);
                    }

                    // ����Ɏq�E�B���h�E��[�@��i�����△�����X�L�b�v�j
                    var deep = ChildWindowFromPointEx(hWnd, pt, CWP_SKIPTRANSPARENT | CWP_SKIPDISABLED);
                    if (deep != IntPtr.Zero)
                        hWnd = deep;

                    string title = GetWindowTextSafe(hWnd);
                    string className = GetClassNameSafe(hWnd);

                    // �^�C�g������Ȃ�g�b�v���x�����Q��
                    if (string.IsNullOrEmpty(title))
                    {
                        var top = GetAncestor(hWnd, GA_ROOT);
                        if (top != IntPtr.Zero && top != hWnd)
                        {
                            var topTitle = GetWindowTextSafe(top);
                            if (!string.IsNullOrEmpty(topTitle))
                                title = topTitle;
                        }
                    }

                    _logger.LogInformation("WindowInfoService: �N���b�N�����E�B���h�E Title={Title}, Class={Class}", title, className);
                    return (title, className);
                }
                await Task.Delay(50, cancellationToken);
            }
            throw new OperationCanceledException();
        }

        #region Win32
        private const int VK_RBUTTON = 0x02;
        private const int VK_ESCAPE = 0x1B;

        private const uint CWP_SKIPDISABLED = 0x0002;
        private const uint CWP_SKIPTRANSPARENT = 0x0004;
        private const uint GA_ROOT = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);
        [DllImport("user32.dll")] private static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, POINT pt, uint uFlags);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private static bool IsKeyOrButtonPressed(int vKey) => (GetAsyncKeyState(vKey) & 0x0001) != 0;

        private static string GetWindowTextSafe(IntPtr hWnd)
        {
            var sb = new StringBuilder(512);
            if (GetWindowText(hWnd, sb, sb.Capacity) > 0)
                return sb.ToString();
            return string.Empty;
        }
        private static string GetClassNameSafe(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            if (GetClassName(hWnd, sb, sb.Capacity) > 0)
                return sb.ToString();
            return string.Empty;
        }
        #endregion
    }
}
