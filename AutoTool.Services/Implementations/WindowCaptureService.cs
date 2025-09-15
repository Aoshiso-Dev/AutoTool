using System.Drawing;
using System.Runtime.InteropServices;
using AutoTool.Services.Abstractions;
using AutoTool.Services.ColorPicking;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Implementations;

/// <summary>
/// ウィンドウキャプチャ専用サービス
/// </summary>
public class WindowCaptureService : IWindowCaptureService
{
    private readonly ILogger<WindowCaptureService> _logger;
    private readonly IMouseService _mouseService;

    public WindowCaptureService(
        ILogger<WindowCaptureService> logger,
        IMouseService mouseService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
    }

    public async Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync()
    {
        var position = await WaitForRightClickAsync();
        if (position == null) return null;

        return GetWindowInfoAt(position.Value);
    }

    public async Task<Point?> CaptureCoordinateAtRightClickAsync()
    {
        return await WaitForRightClickAsync();
    }

    public WindowCaptureResult? GetWindowInfoAt(Point position)
    {
        try
        {
            var hwnd = WindowFromPoint(new POINT { x = position.X, y = position.Y });
            if (hwnd == IntPtr.Zero) return null;

            var titleLength = GetWindowTextLength(hwnd);
            if (titleLength == 0) return null;

            var title = new System.Text.StringBuilder(titleLength + 1);
            GetWindowText(hwnd, title, title.Capacity);

            var className = new System.Text.StringBuilder(256);
            GetClassName(hwnd, className, className.Capacity);

            GetWindowRect(hwnd, out var rect);

            return new WindowCaptureResult
            {
                WindowTitle = title.ToString(),
                WindowClassName = className.ToString(),
                Handle = hwnd,
                Position = new Point(rect.Left, rect.Top),
                Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get window info at position {Position}", position);
            return null;
        }
    }

    private async Task<Point?> WaitForRightClickAsync()
    {
        _logger.LogDebug("Waiting for right click");
        
        while (true)
        {
            if (IsKeyPressed(VK_ESCAPE))
            {
                _logger.LogDebug("Right click wait cancelled");
                return null;
            }

            if (IsKeyPressed(VK_RBUTTON))
            {
                var position = _mouseService.GetCurrentPosition();
                _logger.LogDebug("Right click detected at {Position}", position);
                
                // 右クリックが離されるまで待機
                while (IsKeyPressed(VK_RBUTTON))
                {
                    await Task.Delay(10);
                }
                
                return new Point((int)position.X, (int)position.Y);
            }

            await Task.Delay(50);
        }
    }

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private bool IsKeyPressed(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }

    private const int VK_ESCAPE = 0x1B;
    private const int VK_RBUTTON = 0x02;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion
}