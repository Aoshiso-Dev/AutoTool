using System.Drawing;

namespace MacroPanels.Command.Services;

/// <summary>
/// ウィンドウ操作サービスのインターフェース
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// ウィンドウハンドルを取得します
    /// </summary>
    /// <param name="windowTitle">ウィンドウタイトル</param>
    /// <param name="windowClassName">ウィンドウクラス名</param>
    /// <returns>ウィンドウハンドル</returns>
    IntPtr GetWindowHandle(string? windowTitle, string? windowClassName);

    /// <summary>
    /// ウィンドウの矩形を取得します
    /// </summary>
    /// <param name="windowHandle">ウィンドウハンドル</param>
    /// <returns>ウィンドウの矩形、取得できない場合はnull</returns>
    Rectangle? GetWindowRect(IntPtr windowHandle);

    /// <summary>
    /// 絶対座標をウィンドウ相対座標に変換します
    /// </summary>
    /// <param name="absoluteX">絶対X座標</param>
    /// <param name="absoluteY">絶対Y座標</param>
    /// <param name="windowTitle">ウィンドウタイトル</param>
    /// <param name="windowClassName">ウィンドウクラス名</param>
    /// <returns>相対座標と成功フラグ</returns>
    (int relativeX, int relativeY, bool success, string? errorMessage) ConvertToRelativeCoordinates(
        int absoluteX, int absoluteY, string? windowTitle, string? windowClassName);
}
