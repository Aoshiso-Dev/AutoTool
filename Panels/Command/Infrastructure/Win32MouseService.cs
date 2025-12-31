using System.Windows.Input;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Infrastructure;

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
                await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
                break;
            case MouseButton.Right:
                await MouseHelper.Input.RightClickAsync(x, y, windowTitle, windowClassName);
                break;
            case MouseButton.Middle:
                await MouseHelper.Input.MiddleClickAsync(x, y, windowTitle, windowClassName);
                break;
            default:
                throw new ArgumentException("不正なマウスボタンです。", nameof(button));
        }
    }
}
