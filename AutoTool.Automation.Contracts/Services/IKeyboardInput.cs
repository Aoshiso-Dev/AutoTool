using AutoTool.Commands.Model.Input;

namespace AutoTool.Commands.Services;

/// <summary>
/// キーボード入力のインターフェース
/// </summary>
public interface IKeyboardInput
{
    /// <summary>
    /// キー入力を送信します
    /// </summary>
    /// <param name="key">キー</param>
    /// <param name="ctrl">Ctrlキー押下</param>
    /// <param name="alt">Altキー押下</param>
    /// <param name="shift">Shiftキー押下</param>
    /// <param name="windowTitle">対象ウィンドウのタイトル（オプション）</param>
    /// <param name="windowClassName">対象ウィンドウのクラス名（オプション）</param>
    Task SendKeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null);
}
