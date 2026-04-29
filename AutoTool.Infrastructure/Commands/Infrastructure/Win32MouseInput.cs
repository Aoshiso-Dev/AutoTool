using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// Win32 APIを使用したマウス入力の実装
/// </summary>
public class Win32MouseInput : IMouseInput
{
    public async Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null, int holdDurationMs = 20, string clickInjectionMode = "MouseEvent", bool simulateMouseMove = false, bool restoreCursorPositionAfterClick = false, bool restoreWindowZOrderAfterClick = false)
    {
        await (button switch
        {
            CommandMouseButton.Left => Win32MouseInterop.ClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty, holdDurationMs, clickInjectionMode, simulateMouseMove, restoreCursorPositionAfterClick, restoreWindowZOrderAfterClick),
            CommandMouseButton.Right => Win32MouseInterop.RightClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty, holdDurationMs, clickInjectionMode, simulateMouseMove, restoreCursorPositionAfterClick, restoreWindowZOrderAfterClick),
            CommandMouseButton.Middle => Win32MouseInterop.MiddleClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty, holdDurationMs, clickInjectionMode, simulateMouseMove, restoreCursorPositionAfterClick, restoreWindowZOrderAfterClick),
            _ => throw new ArgumentException("不正なマウスボタンです。", nameof(button))
        });
    }
}

