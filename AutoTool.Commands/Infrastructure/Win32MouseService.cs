using System.Windows.Input;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// Win32 APIを使用したマウス操作サービスの実装
/// </summary>
public class Win32MouseService : IMouseService
{
    public async Task ClickAsync(int x, int y, MouseButton button, string? windowTitle = null, string? windowClassName = null)
    {
        switch (button)
        {
            case MouseButton.Left:
                await Win32MouseInputHelper.ClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty);
                break;
            case MouseButton.Right:
                await Win32MouseInputHelper.RightClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty);
                break;
            case MouseButton.Middle:
                await Win32MouseInputHelper.MiddleClickAsync(x, y, windowTitle ?? string.Empty, windowClassName ?? string.Empty);
                break;
            default:
                throw new ArgumentException("不正なマウスボタンです。", nameof(button));
        }
    }
}

