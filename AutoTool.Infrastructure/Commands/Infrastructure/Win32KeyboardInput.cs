using System.Windows.Input;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// Win32 APIを使用したキーボード入力の実装
/// </summary>
public class Win32KeyboardInput : IKeyboardInput
{
    public Task SendKeyAsync(Key key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null)
    {
        return Task.Run(() => Win32KeyboardInputHelper.KeyPress(key, ctrl, alt, shift, windowTitle ?? string.Empty, windowClassName ?? string.Empty));
    }
}


