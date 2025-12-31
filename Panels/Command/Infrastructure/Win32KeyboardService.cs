using System.Windows.Input;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Infrastructure;

/// <summary>
/// Win32 APIを使用したキーボード操作サービスの実装
/// </summary>
public class Win32KeyboardService : IKeyboardService
{
    public Task SendKeyAsync(Key key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null)
    {
        return Task.Run(() => KeyHelper.Input.KeyPress(key, ctrl, alt, shift, windowTitle, windowClassName));
    }
}
