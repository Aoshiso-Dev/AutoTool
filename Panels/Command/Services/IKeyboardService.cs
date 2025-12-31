using System.Windows.Input;

namespace MacroPanels.Command.Services;

/// <summary>
/// キーボード操作サービスのインターフェース
/// </summary>
public interface IKeyboardService
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
    Task SendKeyAsync(Key key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null);
}
