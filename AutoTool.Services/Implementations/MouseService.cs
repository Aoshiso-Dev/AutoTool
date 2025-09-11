using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace AutoTool.Services.Implementations;

/// <summary>
/// マウスサービスの実装
/// </summary>
public class MouseService : IMouseService
{
    private readonly ILogger<MouseService> _logger;

    public MouseService(ILogger<MouseService> logger)
    {
        _logger = logger;
    }

    public async Task ClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            _logger.LogDebug("Clicking at ({X}, {Y})", x, y);
            SetCursorPos(x, y);
            await Task.Delay(50, cancellationToken);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }, cancellationToken);
    }

    public async Task RightClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            _logger.LogDebug("Right clicking at ({X}, {Y})", x, y);
            SetCursorPos(x, y);
            await Task.Delay(50, cancellationToken);
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }, cancellationToken);
    }

    public async Task DoubleClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            _logger.LogDebug("Double clicking at ({X}, {Y})", x, y);
            SetCursorPos(x, y);
            await Task.Delay(50, cancellationToken);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            await Task.Delay(50, cancellationToken);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }, cancellationToken);
    }

    public async Task MoveAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogDebug("Moving mouse to ({X}, {Y})", x, y);
            SetCursorPos(x, y);
        }, cancellationToken);
    }

    public (int X, int Y) GetCurrentPosition()
    {
        GetCursorPos(out var point);
        return (point.x, point.y);
    }

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    #endregion
}