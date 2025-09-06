using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;

namespace AutoTool.Services.ColorPicking
{
    /// <summary>
    /// 高度なカラーピッキングサービスのインターフェース
    /// </summary>
    public interface IAdvancedColorPickingService : IDisposable
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
        /// 履歴の最大件数
        /// </summary>
        int MaxHistoryCount { get; set; }

        /// <summary>
        /// 指定座標の色を取得します
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>取得した色情報</returns>
        Task<ColorInfo?> GetColorAtPositionAsync(int x, int y);

        /// <summary>
        /// 指定座標の色を取得します
        /// </summary>
        /// <param name="point">座標</param>
        /// <returns>取得した色情報</returns>
        Task<ColorInfo?> GetColorAtPositionAsync(System.Windows.Point point);

        /// <summary>
        /// 現在のマウス位置の色を取得します
        /// </summary>
        /// <returns>マウス位置の色情報</returns>
        Task<ColorInfo?> GetColorAtCurrentMousePositionAsync();

        /// <summary>
        /// 指定領域の平均色を取得します
        /// </summary>
        /// <param name="region">取得領域</param>
        /// <returns>平均色情報</returns>
        Task<ColorInfo?> GetAverageColorInRegionAsync(Rect region);

        /// <summary>
        /// 指定色に最も近い色を画面から検索します
        /// </summary>
        /// <param name="targetColor">検索対象の色</param>
        /// <param name="tolerance">許容差（0-100）</param>
        /// <returns>見つかった色の位置</returns>
        Task<ColorInfo?> FindSimilarColorAsync(Color targetColor, double tolerance = 10.0);

        /// <summary>
        /// 指定領域の色ヒストグラムを取得します
        /// </summary>
        /// <param name="region">解析領域</param>
        /// <returns>色ヒストグラム情報</returns>
        Task<ColorHistogram?> GetColorHistogramAsync(Rect region);

        /// <summary>
        /// 色履歴をクリアします
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// 指定したインデックスの色情報を取得します
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>色情報</returns>
        ColorInfo? GetHistoryAt(int index);

        /// <summary>
        /// 特定の色に近い色を履歴から検索します
        /// </summary>
        /// <param name="targetColor">対象色</param>
        /// <param name="maxDistance">最大距離</param>
        /// <returns>類似色のリスト</returns>
        IEnumerable<ColorInfo> FindSimilarColorsInHistory(Color targetColor, double maxDistance = 50.0);

        /// <summary>
        /// 履歴の統計情報を取得します
        /// </summary>
        /// <returns>統計情報</returns>
        ColorHistoryStatistics GetHistoryStatistics();

        /// <summary>
        /// 履歴から色パレットを生成します
        /// </summary>
        /// <param name="maxColors">最大色数</param>
        /// <returns>色パレット</returns>
        IEnumerable<Color> GenerateColorPalette(int maxColors = 16);

        /// <summary>
        /// 補色パレットを生成します
        /// </summary>
        /// <param name="baseColor">ベース色</param>
        /// <returns>補色パレット</returns>
        IEnumerable<Color> GenerateComplementaryPalette(Color baseColor);

        /// <summary>
        /// ColorをHex文字列に変換します
        /// </summary>
        /// <param name="color">変換する色</param>
        /// <returns>Hex文字列</returns>
        string ColorToHex(Color color);

        /// <summary>
        /// Hex文字列をColorに変換します
        /// </summary>
        /// <param name="hex">Hex文字列</param>
        /// <returns>変換された色</returns>
        Color? HexToColor(string hex);

        /// <summary>
        /// RGB値からHSV値に変換します
        /// </summary>
        /// <param name="color">RGB色</param>
        /// <returns>HSV値</returns>
        (int H, int S, int V) RgbToHsv(Color color);

        /// <summary>
        /// HSV値からRGB色に変換します
        /// </summary>
        /// <param name="h">色相</param>
        /// <param name="s">彩度</param>
        /// <param name="v">明度</param>
        /// <returns>RGB色</returns>
        Color HsvToRgb(int h, int s, int v);

        /// <summary>
        /// 2つの色の類似度を計算します
        /// </summary>
        /// <param name="color1">色1</param>
        /// <param name="color2">色2</param>
        /// <returns>類似度（0-100）</returns>
        double CalculateColorSimilarity(Color color1, Color color2);

        /// <summary>
        /// System.Drawing.ColorからSystem.Windows.Media.Colorに変換します
        /// </summary>
        /// <param name="drawingColor">変換元の色</param>
        /// <returns>変換された色</returns>
        System.Windows.Media.Color ToMediaColor(Color drawingColor);

        /// <summary>
        /// System.Windows.Media.ColorからSystem.Drawing.Colorに変換します
        /// </summary>
        /// <param name="mediaColor">変換元の色</param>
        /// <returns>変換された色</returns>
        Color ToDrawingColor(System.Windows.Media.Color mediaColor);
    }
}