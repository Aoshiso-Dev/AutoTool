using System.Windows.Input;

namespace AutoTool.Commands.Services;

/// <summary>
/// マウス入力のインターフェース
/// </summary>
public interface IMouseInput
{
    /// <summary>
    /// 指定座標でマウスクリックを実行します
    /// </summary>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    /// <param name="button">マウスボタン</param>
    /// <param name="windowTitle">対象ウィンドウのタイトル（オプション）</param>
    /// <param name="windowClassName">対象ウィンドウのクラス名（オプション）</param>
    Task ClickAsync(int x, int y, MouseButton button, string? windowTitle = null, string? windowClassName = null);
}
