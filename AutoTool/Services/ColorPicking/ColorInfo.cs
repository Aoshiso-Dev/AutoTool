using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;

namespace AutoTool.Services.ColorPicking
{
    /// <summary>
    /// 色情報を表すクラス
    /// </summary>
    public class ColorInfo
    {
        /// <summary>
        /// 取得した色
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// 取得位置
        /// </summary>
        public System.Windows.Point Position { get; set; }

        /// <summary>
        /// 取得時刻
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 平均色かどうか
        /// </summary>
        public bool IsAverageColor { get; set; } = false;

        /// <summary>
        /// サンプリング領域（平均色の場合）
        /// </summary>
        public Rect? SampleRegion { get; set; }

        /// <summary>
        /// マッチ度（類似色検索の場合）
        /// </summary>
        public double? MatchScore { get; set; }

        /// <summary>
        /// 16進数カラーコード
        /// </summary>
        public string HexCode => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

        /// <summary>
        /// RGB文字列
        /// </summary>
        public string RgbString => $"RGB({Color.R}, {Color.G}, {Color.B})";

        /// <summary>
        /// HSV値
        /// </summary>
        public (int H, int S, int V) HSV
        {
            get
            {
                var max = Math.Max(Color.R, Math.Max(Color.G, Color.B));
                var min = Math.Min(Color.R, Math.Min(Color.G, Color.B));
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
                else if (max == Color.R)
                {
                    h = (int)(60 * ((Color.G - Color.B) / (double)delta) % 360);
                }
                else if (max == Color.G)
                {
                    h = (int)(60 * ((Color.B - Color.R) / (double)delta) + 120);
                }
                else
                {
                    h = (int)(60 * ((Color.R - Color.G) / (double)delta) + 240);
                }

                if (h < 0) h += 360;

                return (h, s, v);
            }
        }

        /// <summary>
        /// HSV文字列
        /// </summary>
        public string HsvString
        {
            get
            {
                var hsv = HSV;
                return $"HSV({hsv.H}°, {hsv.S}%, {hsv.V}%)";
            }
        }

        /// <summary>
        /// 色の明度を取得
        /// </summary>
        public double Brightness => (Color.R * 0.299 + Color.G * 0.587 + Color.B * 0.114) / 255.0;

        /// <summary>
        /// 色が明るいかどうか
        /// </summary>
        public bool IsLightColor => Brightness > 0.5;

        /// <summary>
        /// 補色を取得
        /// </summary>
        public Color ComplementaryColor => Color.FromArgb(255 - Color.R, 255 - Color.G, 255 - Color.B);

        public override string ToString()
        {
            var result = $"{HexCode} ({RgbString})";
            if (IsAverageColor && SampleRegion.HasValue)
            {
                result += " [平均色]";
            }
            if (MatchScore.HasValue)
            {
                result += $" [一致度: {MatchScore.Value:F1}%]";
            }
            return result;
        }

        /// <summary>
        /// 2つの色の距離を計算（ユークリッド距離）
        /// </summary>
        public double DistanceTo(ColorInfo other)
        {
            var rDiff = Color.R - other.Color.R;
            var gDiff = Color.G - other.Color.G;
            var bDiff = Color.B - other.Color.B;
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        /// <summary>
        /// ColorInfoを System.Windows.Media.Color に変換
        /// </summary>
        public System.Windows.Media.Color ToMediaColor()
        {
            return System.Windows.Media.Color.FromArgb(Color.A, Color.R, Color.G, Color.B);
        }

        /// <summary>
        /// System.Drawing.Color から ColorInfo を作成
        /// </summary>
        public static ColorInfo FromColor(Color color, System.Windows.Point position)
        {
            return new ColorInfo
            {
                Color = color,
                Position = position,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// Hex文字列から ColorInfo を作成
        /// </summary>
        public static ColorInfo? FromHex(string hex, System.Windows.Point position)
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

                return new ColorInfo
                {
                    Color = Color.FromArgb(r, g, b),
                    Position = position,
                    Timestamp = DateTime.Now
                };
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 色ヒストグラム情報
    /// </summary>
    public class ColorHistogram
    {
        /// <summary>
        /// 支配的な色
        /// </summary>
        public Color DominantColor { get; set; }

        /// <summary>
        /// 解析領域
        /// </summary>
        public Rect Region { get; set; }

        /// <summary>
        /// 信頼度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 解析時刻
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 支配的な色の16進数コード
        /// </summary>
        public string DominantColorHex => $"#{DominantColor.R:X2}{DominantColor.G:X2}{DominantColor.B:X2}";

        /// <summary>
        /// 支配的な色のColorInfo
        /// </summary>
        public ColorInfo DominantColorInfo
        {
            get
            {
                return new ColorInfo
                {
                    Color = DominantColor,
                    Position = new System.Windows.Point(Region.X + Region.Width / 2, Region.Y + Region.Height / 2),
                    Timestamp = Timestamp,
                    SampleRegion = Region
                };
            }
        }

        public override string ToString()
        {
            return $"支配的な色: {DominantColorHex} (信頼度: {Confidence:F2})";
        }
    }

    /// <summary>
    /// 色履歴の統計情報
    /// </summary>
    public class ColorHistoryStatistics
    {
        public int TotalColors { get; set; }
        public Color AverageColor { get; set; }
        public Color MostCommonColor { get; set; }
        public DateTime EarliestPickTime { get; set; }
        public DateTime LatestPickTime { get; set; }
        
        public TimeSpan TimeSpan => LatestPickTime - EarliestPickTime;
        
        public double PickRate => TotalColors > 0 ? TimeSpan.TotalMinutes / TotalColors : 0;
    }
}