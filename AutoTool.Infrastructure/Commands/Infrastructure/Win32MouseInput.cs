using System.Windows.Input;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// Win32 APIを使用したマウス入力の実装
/// </summary>
public class Win32MouseInput : IMouseInput
{
    public async Task ClickAsync(int x, int y, MouseButton button, string? windowTitle = null, string? windowClassName = null)
    {
        await (button switch
        {
            MouseButton.Left => Win32MouseInterop.ClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty),
            MouseButton.Right => Win32MouseInterop.RightClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty),
            MouseButton.Middle => Win32MouseInterop.MiddleClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty),
            _ => throw new ArgumentException("不正なマウスボタンです。", nameof(button))
        });
    }
}

