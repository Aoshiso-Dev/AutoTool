using System.Drawing;
using AutoTool.Services.ColorPicking;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// 高度な色選択機能サービスのインターフェース
/// </summary>
public interface IAdvancedColorPickingService
{
    /// <summary>
    /// 色の履歴
    /// </summary>
    IReadOnlyList<ColorInfo> ColorHistory { get; }

    /// <summary>
    /// 最後に取得した色情報
    /// </summary>
    ColorInfo? LastColorInfo { get; }

    /// <summary>
    /// 指定座標の色情報を取得
    /// </summary>
    Task<ColorInfo?> GetColorInfoAtPositionAsync(int x, int y);

    /// <summary>
    /// 現在のマウス位置の色情報を取得
    /// </summary>
    Task<ColorInfo?> GetColorInfoAtCurrentMousePositionAsync();

    /// <summary>
    /// 指定領域の平均色を取得
    /// </summary>
    Task<ColorInfo?> GetAverageColorInRegionAsync(System.Windows.Rect region);

    /// <summary>
    /// 類似色を検索
    /// </summary>
    Task<ColorInfo?> FindSimilarColorAsync(Color targetColor, double tolerance = 10.0);

    /// <summary>
    /// 色ヒストグラムを生成
    /// </summary>
    Task<ColorHistogram?> GetColorHistogramAsync(System.Windows.Rect region);

    /// <summary>
    /// 色履歴の統計を取得
    /// </summary>
    ColorHistoryStatistics GetColorHistoryStatistics();

    /// <summary>
    /// 色履歴をクリア
    /// </summary>
    void ClearColorHistory();

    /// <summary>
    /// 使用頻度の高い色パレットを生成
    /// </summary>
    IEnumerable<Color> GenerateColorPalette(int maxColors = 16);

    /// <summary>
    /// 補色パレットを生成
    /// </summary>
    IEnumerable<Color> GenerateComplementaryPalette(Color baseColor);
}