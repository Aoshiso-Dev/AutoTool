using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.View.Converter
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool b && b)
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NumberToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToTextConverter : IValueConverter
    {
        public string TrueText { get; set; } = "True";
        public string FalseText { get; set; } = "False";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? TrueText : FalseText;
            return FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class KeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Input.Key key)
                return key.ToString();
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && Enum.TryParse<System.Windows.Input.Key>(str, out var key))
                return key;
            return System.Windows.Input.Key.None;
        }
    }

    public class NestLevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int nestLevel)
                return new Thickness(nestLevel * 20, 0, 0, 0);
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// コマンドタイプを日本語表示名に変換するコンバーター
    /// </summary>
    public class CommandTypeToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string commandType && !string.IsNullOrEmpty(commandType))
            {
                // CommandRegistryの日本語表示名を取得
                return CommandRegistry.DisplayOrder.GetDisplayName(commandType);
            }
            return value ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 日本語表示名から英語のコマンドタイプに戻す（必要に応じて実装）
            throw new NotImplementedException("ConvertBack is not supported for CommandTypeToDisplayNameConverter");
        }
    }

    /// <summary>
    /// コマンドタイプからカテゴリ色を取得するコンバーター
    /// </summary>
    public class CommandTypeToCategoryColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string commandType && !string.IsNullOrEmpty(commandType))
            {
                var priority = CommandRegistry.DisplayOrder.GetPriority(commandType);
                return priority switch
                {
                    1 => new SolidColorBrush(Color.FromRgb(135, 206, 235)), // スカイブルー (クリック操作)
                    2 => new SolidColorBrush(Color.FromRgb(152, 251, 152)), // ライトグリーン (基本操作)
                    3 => new SolidColorBrush(Color.FromRgb(255, 218, 185)), // ピーチパフ (ループ制御)
                    4 => new SolidColorBrush(Color.FromRgb(221, 160, 221)), // プラム (条件分岐)
                    5 => new SolidColorBrush(Color.FromRgb(255, 228, 196)), // ビスク (変数操作)
                    6 => new SolidColorBrush(Color.FromRgb(192, 192, 192)), // シルバー (システム操作)
                    _ => new SolidColorBrush(Color.FromRgb(255, 255, 255))  // ホワイト (その他)
                };
            }
            return new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
