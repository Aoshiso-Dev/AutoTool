using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.View.Converter;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class BoolToVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.OfType<bool>().Any(b => b) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}

public class NumberToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int intValue && intValue > 0;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class BooleanToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "True";
    public string FalseText { get; set; } = "False";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? TrueText : FalseText;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string str && !string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class KeyToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is System.Windows.Input.Key key ? key.ToString() : string.Empty;
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
        return value is int nestLevel ? new Thickness(nestLevel * 20, 0, 0, 0) : new Thickness(0);
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
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
            return CommandRegistry.DisplayOrder.GetDisplayName(commandType);
        }
        return value ?? string.Empty;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// コマンドタイプからカテゴリ色を取得するコンバーター
/// </summary>
public class CommandTypeToCategoryColorConverter : IValueConverter
{
    private static readonly SolidColorBrush SkyBlue = new(Color.FromRgb(135, 206, 235));
    private static readonly SolidColorBrush LightGreen = new(Color.FromRgb(152, 251, 152));
    private static readonly SolidColorBrush PeachPuff = new(Color.FromRgb(255, 218, 185));
    private static readonly SolidColorBrush Plum = new(Color.FromRgb(221, 160, 221));
    private static readonly SolidColorBrush Bisque = new(Color.FromRgb(255, 228, 196));
    private static readonly SolidColorBrush Silver = new(Color.FromRgb(192, 192, 192));
    private static readonly SolidColorBrush White = new(Color.FromRgb(255, 255, 255));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string commandType && !string.IsNullOrEmpty(commandType))
        {
            var priority = CommandRegistry.DisplayOrder.GetPriority(commandType);
            return priority switch
            {
                1 => SkyBlue,      // クリック操作
                2 => LightGreen,   // 基本操作
                3 => PeachPuff,    // ループ制御
                4 => Plum,         // 条件分岐
                5 => Bisque,       // 変数操作
                6 => Silver,       // システム操作
                _ => White         // その他
            };
        }
        return White;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// プログレス値(0-100)を指定された最大幅にスケーリングするコンバーター
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int progress && parameter is double maxWidth)
        {
            return maxWidth * progress / 100.0;
        }
        if (value is int progressInt)
        {
            // パラメータがない場合はデフォルト50px
            var max = 50.0;
            if (parameter is string paramStr && double.TryParse(paramStr, out var parsed))
            {
                max = parsed;
            }
            return max * progressInt / 100.0;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
