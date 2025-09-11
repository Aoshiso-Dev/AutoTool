using System.Drawing;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// 色選択サービスのインターフェース
/// </summary>
public interface IColorPickService
{
    /// <summary>
    /// スクリーンカラーピッカーを表示して画面上の色を取得します
    /// </summary>
    Task<Color?> CaptureColorFromScreenAsync();

    /// <summary>
    /// 右クリック位置の色を取得
    /// </summary>
    Task<Color?> CaptureColorAtRightClickAsync();

    /// <summary>
    /// 指定された座標の色を取得します
    /// </summary>
    Color GetColorAt(int x, int y);

    /// <summary>
    /// 指定された座標の色を取得します
    /// </summary>
    Color GetColorAt(Point position);

    /// <summary>
    /// 現在のマウス位置の色を取得します
    /// </summary>
    Color GetColorAtCurrentMousePosition();

    /// <summary>
    /// Colorを16進数文字列に変換します
    /// </summary>
    string ColorToHex(Color color);

    /// <summary>
    /// 16進数文字列からColorに変換します
    /// </summary>
    Color? HexToColor(string hex);

    /// <summary>
    /// System.Drawing.ColorからSystem.Windows.Media.Colorに変換します
    /// </summary>
    System.Windows.Media.Color ToMediaColor(Color drawingColor);

    /// <summary>
    /// System.Windows.Media.ColorからSystem.Drawing.Colorに変換します
    /// </summary>
    Color ToDrawingColor(System.Windows.Media.Color mediaColor);

    /// <summary>
    /// カラーピッカーが現在アクティブかどうかを取得します
    /// </summary>
    bool IsColorPickerActive { get; }

    /// <summary>
    /// カラーピッカーをキャンセルします
    /// </summary>
    void CancelColorPicker();
}