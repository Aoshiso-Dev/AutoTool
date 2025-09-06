using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Collections;
using System.Windows.Media;

namespace AutoTool.View.Converters
{
    /// <summary>
    /// Boolean値をVisibilityに変換するコンバーター
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Multiple Boolean値をORでVisibilityに変換するコンバーター
    /// ConverterParameter="Not"でNOT演算を適用可能
    /// </summary>
    public class MultiBoolToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) return Visibility.Collapsed;

            bool result = false;
            foreach (var value in values)
            {
                if (value is bool boolValue && boolValue)
                {
                    result = true;
                    break;
                }
            }

            // パラメーターで"Not"が指定されている場合は反転
            if (parameter is string param && param.Equals("Not", StringComparison.OrdinalIgnoreCase))
            {
                result = !result;
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Boolean値を反転するコンバーター
    /// </summary>
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }
    }

    /// <summary>
    /// 実行状態をテキストに変換するコンバーター
    /// </summary>
    public class RunningStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? "実行中" : "停止中";
            }
            return "不明";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Null判定をBooleanに変換するコンバーター
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Enum値を表示名に変換するコンバーター
    /// </summary>
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return enumValue.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, stringValue);
                }
                catch
                {
                    return Enum.GetValues(targetType).GetValue(0);
                }
            }
            return null;
        }
    }

    /// <summary>
    /// ネストレベルを左マージンに変換するコンバーター
    /// </summary>
    public class NestLevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int nestLevel)
            {
                var leftMargin = nestLevel * 20; // ネストレベル1あたり20pxインデント
                return new Thickness(leftMargin, 2, 2, 2);
            }
            return new Thickness(2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ネストレベルを背景色に変換するコンバーター
    /// </summary>
    public class NestLevelToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int nestLevel)
            {
                return nestLevel switch
                {
                    0 => System.Windows.Media.Brushes.Transparent,
                    1 => new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 100, 200)), // 薄い青
                    2 => new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 150, 100)), // 薄い緑
                    3 => new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 200, 100, 0)), // 薄いオレンジ
                    _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 150, 0, 150))   // 薄い紫
                };
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Progress値をProgressBar用に変換するコンバーター（0-100の範囲）
    /// </summary>
    public class ProgressToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int progress)
            {
                return Math.Max(0, Math.Min(100, progress));
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態とプログレスを組み合わせてProgressBar表示を制御するコンバーター
    /// </summary>
    public class RunningStateToProgressVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool isRunning && values[1] is int progress)
            {
                return isRunning && progress > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// コマンドタイプを絵文字アイコンに変換するコンバーター（実行状態対応）
    /// </summary>
    public class CommandTypeToIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 3 && values[0] is string itemType)
            {
                var isRunning = values[1] is bool running && running;
                var isEnabled = values[2] is bool enabled && enabled;

                // 実行中の場合は特別なアイコン
                if (isRunning)
                {
                    return "▶️"; // 実行中アイコン
                }

                // 無効な場合はグレーアウトされた感じのアイコン
                if (!isEnabled)
                {
                    return "⚫"; // 無効アイコン
                }

                // 通常のコマンドタイプ別アイコン
                return itemType switch
                {
                    "Wait_Image" => "⏱️",
                    "ClickImage" => "🖱️",
                    "ClickImage_AI" => "🤖",
                    "Hotkey" => "⌨️",
                    "Click" => "👆",
                    "Wait" => "⏸️",
                    "Loop" => "🔄",
                    "LoopEnd" => "🔚",
                    "LoopBreak" => "⚡",
                    "IfImageExist" => "❓",
                    "IfImageNotExist" => "❗",
                    "IfImageExist_AI" => "🔍",
                    "IfImageNotExist_AI" => "🔍",
                    "IfEnd" => "✅",
                    "IfVariable" => "📊",
                    "Execute" => "🚀",
                    "SetVariable" => "📝",
                    "SetVariableAI" => "🧠",
                    "Screenshot" => "📸",
                    _ => "📄"
                };
            }
            return "📄";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 単一値版のコマンドタイプアイコンコンバーター（後方互換性用）
    /// </summary>
    public class CommandTypeToIconSingleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string itemType)
            {
                return itemType switch
                {
                    "WaitImage" => "⏱️",
                    "ClickImage" => "🖱️",
                    "ClickImage_AI" => "🤖",
                    "Hotkey" => "⌨️",
                    "Click" => "👆",
                    "Wait" => "⏸️",
                    "Loop" => "🔄",
                    "LoopEnd" => "🔚",
                    "LoopBreak" => "⚡",
                    "IfImageExist" => "❓",
                    "IfImageNotExist" => "❗",
                    "IfImageExistAI" => "🔍",
                    "IfImageNotExistAI" => "🔍",
                    "IfEnd" => "✅",
                    "IfVariable" => "📊",
                    "Execute" => "🚀",
                    "SetVariable" => "📝",
                    "SetVariableAI" => "🧠",
                    "Screenshot" => "📸",
                    _ => "📄"
                };
            }
            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態をハイライト色に変換するコンバーター
    /// </summary>
    public class RunningHighlightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning && isRunning)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)); // 青いハイライト
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態をボーダー太さに変換するコンバーター
    /// </summary>
    public class RunningBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning && isRunning)
            {
                return new Thickness(2);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態をフォント太さに変換するコンバーター
    /// </summary>
    public class RunningFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning && isRunning)
            {
                return FontWeights.Bold;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態をテキスト色に変換するコンバーター
    /// </summary>
    public class RunningTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning && isRunning)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)); // 青いテキスト
            }
            return System.Windows.Media.Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
