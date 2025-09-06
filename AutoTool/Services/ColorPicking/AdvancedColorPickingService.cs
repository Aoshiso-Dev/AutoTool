using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.ColorPicking
{
    /// <summary>
    /// 高度なカラーピッキングサービスの実装
    /// </summary>
    public class AdvancedColorPickingService : IAdvancedColorPickingService, IDisposable
    {
        private readonly ILogger<AdvancedColorPickingService> _logger;
        private readonly List<ColorInfo> _colorHistory;
        private bool _disposed = false;

        /// <summary>
        /// 色の履歴
        /// </summary>
        public IReadOnlyList<ColorInfo> ColorHistory => _colorHistory.AsReadOnly();

        /// <summary>
        /// 最後に取得した色情報
        /// </summary>
        public ColorInfo? LastColorInfo { get; private set; }

        /// <summary>
        /// 履歴の最大件数
        /// </summary>
        public int MaxHistoryCount { get; set; } = 100;

        public AdvancedColorPickingService(ILogger<AdvancedColorPickingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _colorHistory = new List<ColorInfo>();
            
            _logger.LogInformation("AdvancedColorPickingService が初期化されました");
        }

        #region 基本的なカラーピッキング機能

        /// <summary>
        /// 指定座標の色を取得します
        /// </summary>
        public async Task<ColorInfo?> GetColorAtPositionAsync(int x, int y)
        {
            try
            {
                await Task.CompletedTask; // 非同期メソッドの形を保つ
                
                using var bitmap = new Bitmap(1, 1);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                
                var color = bitmap.GetPixel(0, 0);
                var colorInfo = new ColorInfo
                {
                    Color = color,
                    Position = new System.Windows.Point(x, y),
                    Timestamp = DateTime.Now
                };

                AddToHistory(colorInfo);
                _logger.LogDebug("色情報取得: {ColorInfo}", colorInfo);
                
                return colorInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色取得エラー: 座標 ({X}, {Y})", x, y);
                return null;
            }
        }

        /// <summary>
        /// 指定座標の色を取得します
        /// </summary>
        public async Task<ColorInfo?> GetColorAtPositionAsync(System.Windows.Point point)
        {
            return await GetColorAtPositionAsync((int)point.X, (int)point.Y);
        }

        /// <summary>
        /// 現在のマウス位置の色を取得します
        /// </summary>
        public async Task<ColorInfo?> GetColorAtCurrentMousePositionAsync()
        {
            try
            {
                var cursorPos = System.Windows.Forms.Cursor.Position;
                return await GetColorAtPositionAsync(cursorPos.X, cursorPos.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置の色取得エラー");
                return null;
            }
        }

        /// <summary>
        /// 指定領域の平均色を取得します
        /// </summary>
        public async Task<ColorInfo?> GetAverageColorInRegionAsync(Rect region)
        {
            try
            {
                await Task.CompletedTask; // 非同期メソッドの形を保つ
                
                var width = (int)region.Width;
                var height = (int)region.Height;
                var x = (int)region.X;
                var y = (int)region.Y;

                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                long totalR = 0, totalG = 0, totalB = 0;
                int pixelCount = 0;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        var pixel = bitmap.GetPixel(i, j);
                        totalR += pixel.R;
                        totalG += pixel.G;
                        totalB += pixel.B;
                        pixelCount++;
                    }
                }

                var avgColor = Color.FromArgb(
                    (int)(totalR / pixelCount),
                    (int)(totalG / pixelCount),
                    (int)(totalB / pixelCount)
                );

                var colorInfo = new ColorInfo
                {
                    Color = avgColor,
                    Position = new System.Windows.Point(region.X + region.Width / 2, region.Y + region.Height / 2),
                    Timestamp = DateTime.Now,
                    IsAverageColor = true,
                    SampleRegion = region
                };

                AddToHistory(colorInfo);
                _logger.LogDebug("領域平均色取得: {ColorInfo}", colorInfo);
                
                return colorInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "領域平均色取得エラー: {Region}", region);
                return null;
            }
        }

        /// <summary>
        /// 指定色に最も近い色を画面から検索します
        /// </summary>
        public async Task<ColorInfo?> FindSimilarColorAsync(Color targetColor, double tolerance = 10.0)
        {
            try
            {
                await Task.CompletedTask; // 非同期メソッドの形を保つ
                
                // 簡易実装: 画面全体をサンプリング
                var screenWidth = (int)SystemParameters.VirtualScreenWidth;
                var screenHeight = (int)SystemParameters.VirtualScreenHeight;
                
                var step = 10; // サンプリング間隔
                ColorInfo? closestMatch = null;
                double closestDistance = double.MaxValue;

                for (int x = 0; x < screenWidth; x += step)
                {
                    for (int y = 0; y < screenHeight; y += step)
                    {
                        try
                        {
                            using var bitmap = new Bitmap(1, 1);
                            using var graphics = Graphics.FromImage(bitmap);
                            graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                            
                            var pixel = bitmap.GetPixel(0, 0);
                            var distance = CalculateColorDistance(targetColor, pixel);
                            
                            if (distance < closestDistance && distance <= tolerance * 255 / 100)
                            {
                                closestDistance = distance;
                                closestMatch = new ColorInfo
                                {
                                    Color = pixel,
                                    Position = new System.Windows.Point(x, y),
                                    Timestamp = DateTime.Now,
                                    MatchScore = (255 - distance) * 100 / 255
                                };
                            }
                        }
                        catch
                        {
                            // スキップ
                        }
                    }
                }

                if (closestMatch != null)
                {
                    AddToHistory(closestMatch);
                    _logger.LogDebug("類似色検索成功: {ColorInfo}", closestMatch);
                }
                
                return closestMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "類似色検索エラー: {TargetColor}", ColorToHex(targetColor));
                return null;
            }
        }

        /// <summary>
        /// 指定領域の色ヒストグラムを取得します
        /// </summary>
        public async Task<ColorHistogram?> GetColorHistogramAsync(Rect region)
        {
            try
            {
                await Task.CompletedTask; // 非同期メソッドの形を保つ
                
                var width = (int)region.Width;
                var height = (int)region.Height;
                var x = (int)region.X;
                var y = (int)region.Y;

                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                var colorCounts = new Dictionary<Color, int>();

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        var pixel = bitmap.GetPixel(i, j);
                        if (colorCounts.ContainsKey(pixel))
                        {
                            colorCounts[pixel]++;
                        }
                        else
                        {
                            colorCounts[pixel] = 1;
                        }
                    }
                }

                var dominantColor = colorCounts.OrderByDescending(kvp => kvp.Value).First();
                var confidence = (double)dominantColor.Value / (width * height) * 100;

                var histogram = new ColorHistogram
                {
                    DominantColor = dominantColor.Key,
                    Region = region,
                    Confidence = confidence,
                    Timestamp = DateTime.Now
                };

                _logger.LogDebug("色ヒストグラム解析: {Histogram}", histogram);
                return histogram;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色ヒストグラム解析エラー: {Region}", region);
                return null;
            }
        }

        #endregion

        #region 色変換・ユーティリティ機能

        /// <summary>
        /// ColorをHex文字列に変換します
        /// </summary>
        public string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Hex文字列をColorに変換します
        /// </summary>
        public Color? HexToColor(string hex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex))
                    return null;

                hex = hex.TrimStart('#');
                if (hex.Length != 6)
                    return null;

                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);

                return Color.FromArgb(r, g, b);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// RGB値からHSV値に変換します
        /// </summary>
        public (int H, int S, int V) RgbToHsv(Color color)
        {
            var max = Math.Max(color.R, Math.Max(color.G, color.B));
            var min = Math.Min(color.R, Math.Min(color.G, color.B));
            var delta = max - min;

            // Value
            var v = (int)(max * 100.0 / 255.0);

            // Saturation
            var s = max == 0 ? 0 : (int)(delta * 100.0 / max);

            // Hue
            int h;
            if (delta == 0)
            {
                h = 0;
            }
            else if (max == color.R)
            {
                h = (int)(60 * ((color.G - color.B) / (double)delta) % 360);
            }
            else if (max == color.G)
            {
                h = (int)(60 * ((color.B - color.R) / (double)delta) + 120);
            }
            else
            {
                h = (int)(60 * ((color.R - color.G) / (double)delta) + 240);
            }

            if (h < 0) h += 360;

            return (h, s, v);
        }

        /// <summary>
        /// HSV値からRGB色に変換します
        /// </summary>
        public Color HsvToRgb(int h, int s, int v)
        {
            var hf = h / 60.0;
            var sf = s / 100.0;
            var vf = v / 100.0;

            var c = vf * sf;
            var x = c * (1 - Math.Abs((hf % 2) - 1));
            var m = vf - c;

            double r, g, b;

            if (hf >= 0 && hf < 1)
            {
                r = c; g = x; b = 0;
            }
            else if (hf >= 1 && hf < 2)
            {
                r = x; g = c; b = 0;
            }
            else if (hf >= 2 && hf < 3)
            {
                r = 0; g = c; b = x;
            }
            else if (hf >= 3 && hf < 4)
            {
                r = 0; g = x; b = c;
            }
            else if (hf >= 4 && hf < 5)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            var rByte = (int)((r + m) * 255);
            var gByte = (int)((g + m) * 255);
            var bByte = (int)((b + m) * 255);

            return Color.FromArgb(rByte, gByte, bByte);
        }

        /// <summary>
        /// 2つの色の類似度を計算します
        /// </summary>
        public double CalculateColorSimilarity(Color color1, Color color2)
        {
            var rDiff = Math.Abs(color1.R - color2.R);
            var gDiff = Math.Abs(color1.G - color2.G);
            var bDiff = Math.Abs(color1.B - color2.B);

            var maxDiff = Math.Max(rDiff, Math.Max(gDiff, bDiff));
            return (255 - maxDiff) * 100.0 / 255.0;
        }

        /// <summary>
        /// System.Drawing.ColorからSystem.Windows.Media.Colorに変換します
        /// </summary>
        public System.Windows.Media.Color ToMediaColor(Color drawingColor)
        {
            return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }

        /// <summary>
        /// System.Windows.Media.ColorからSystem.Drawing.Colorに変換します
        /// </summary>
        public Color ToDrawingColor(System.Windows.Media.Color mediaColor)
        {
            return Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }

        #endregion

        #region 履歴管理

        /// <summary>
        /// 色情報を履歴に追加します
        /// </summary>
        private void AddToHistory(ColorInfo colorInfo)
        {
            LastColorInfo = colorInfo;
            
            // 既存の同じ色を削除（重複排除）
            var existingIndex = _colorHistory.FindIndex(c => 
                c.Color.R == colorInfo.Color.R && 
                c.Color.G == colorInfo.Color.G && 
                c.Color.B == colorInfo.Color.B);
            
            if (existingIndex >= 0)
            {
                _colorHistory.RemoveAt(existingIndex);
            }

            // 新しい色を先頭に追加
            _colorHistory.Insert(0, colorInfo);

            // 履歴サイズ制限
            while (_colorHistory.Count > MaxHistoryCount)
            {
                _colorHistory.RemoveAt(_colorHistory.Count - 1);
            }

            _logger.LogTrace("色履歴に追加: {ColorInfo}, 履歴数: {Count}", colorInfo, _colorHistory.Count);
        }

        /// <summary>
        /// 色履歴をクリアします
        /// </summary>
        public void ClearHistory()
        {
            _colorHistory.Clear();
            LastColorInfo = null;
            _logger.LogInformation("色履歴をクリアしました");
        }

        /// <summary>
        /// 指定したインデックスの色情報を取得します
        /// </summary>
        public ColorInfo? GetHistoryAt(int index)
        {
            if (index >= 0 && index < _colorHistory.Count)
            {
                return _colorHistory[index];
            }
            return null;
        }

        /// <summary>
        /// 特定の色に近い色を履歴から検索します
        /// </summary>
        public IEnumerable<ColorInfo> FindSimilarColorsInHistory(Color targetColor, double maxDistance = 50.0)
        {
            var targetColorInfo = ColorInfo.FromColor(targetColor, new System.Windows.Point(0, 0));
            
            return _colorHistory
                .Where(c => c.DistanceTo(targetColorInfo) <= maxDistance)
                .OrderBy(c => c.DistanceTo(targetColorInfo));
        }

        /// <summary>
        /// 履歴の統計情報を取得します
        /// </summary>
        public ColorHistoryStatistics GetHistoryStatistics()
        {
            if (!_colorHistory.Any())
            {
                return new ColorHistoryStatistics();
            }

            var colors = _colorHistory.Select(c => c.Color).ToList();
            
            var avgR = colors.Average(c => c.R);
            var avgG = colors.Average(c => c.G);
            var avgB = colors.Average(c => c.B);

            var mostCommonColor = colors
                .GroupBy(c => new { c.R, c.G, c.B })
                .OrderByDescending(g => g.Count())
                .First().Key;

            return new ColorHistoryStatistics
            {
                TotalColors = _colorHistory.Count,
                AverageColor = Color.FromArgb((int)avgR, (int)avgG, (int)avgB),
                MostCommonColor = Color.FromArgb(mostCommonColor.R, mostCommonColor.G, mostCommonColor.B),
                EarliestPickTime = _colorHistory.Min(c => c.Timestamp),
                LatestPickTime = _colorHistory.Max(c => c.Timestamp)
            };
        }

        #endregion

        #region パレット機能

        /// <summary>
        /// 履歴から色パレットを生成します
        /// </summary>
        public IEnumerable<Color> GenerateColorPalette(int maxColors = 16)
        {
            if (!_colorHistory.Any())
            {
                return Enumerable.Empty<Color>();
            }

            var colors = _colorHistory.Select(c => c.Color).Distinct().ToList();
            
            if (colors.Count <= maxColors)
            {
                return colors;
            }

            // 色相ベースでクラスタリング
            var hsvColors = colors.Select(c => new { Color = c, HSV = RgbToHsv(c) }).ToList();
            var palette = new List<Color>();

            // 色相を均等に分割
            var hueStep = 360.0 / maxColors;
            for (int i = 0; i < maxColors; i++)
            {
                var targetHue = i * hueStep;
                var closestColor = hsvColors
                    .OrderBy(c => Math.Abs(c.HSV.H - targetHue))
                    .First().Color;
                
                if (!palette.Contains(closestColor))
                {
                    palette.Add(closestColor);
                }
            }

            return palette;
        }

        /// <summary>
        /// 補色パレットを生成します
        /// </summary>
        public IEnumerable<Color> GenerateComplementaryPalette(Color baseColor)
        {
            var hsv = RgbToHsv(baseColor);
            var palette = new List<Color> { baseColor };

            // 補色
            var complementaryHue = (hsv.H + 180) % 360;
            palette.Add(HsvToRgb(complementaryHue, hsv.S, hsv.V));

            // 三角配色
            var triad1Hue = (hsv.H + 120) % 360;
            var triad2Hue = (hsv.H + 240) % 360;
            palette.Add(HsvToRgb(triad1Hue, hsv.S, hsv.V));
            palette.Add(HsvToRgb(triad2Hue, hsv.S, hsv.V));

            // 明度バリエーション
            for (int i = 1; i <= 3; i++)
            {
                var lighterV = Math.Min(100, hsv.V + i * 20);
                var darkerV = Math.Max(0, hsv.V - i * 20);
                
                palette.Add(HsvToRgb(hsv.H, hsv.S, lighterV));
                palette.Add(HsvToRgb(hsv.H, hsv.S, darkerV));
            }

            return palette.Distinct();
        }

        #endregion

        #region プライベートメソッド

        private double CalculateColorDistance(Color color1, Color color2)
        {
            var rDiff = color1.R - color2.R;
            var gDiff = color1.G - color2.G;
            var bDiff = color1.B - color2.B;
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                _logger.LogInformation("AdvancedColorPickingService がリソース解放されました");
            }
        }

        #endregion
    }
}