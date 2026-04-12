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
        switch (button)
        {
            case MouseButton.Left:
                await Win32MouseInterop.ClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty);
                break;
            case MouseButton.Right:
                await Win32MouseInterop.RightClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty);
                break;
            case MouseButton.Middle:
                await Win32MouseInterop.MiddleClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty);
                break;
            default:
                throw new ArgumentException("不正なマウスボタンです。", nameof(button));
        }
    }
}

