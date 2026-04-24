using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Collections;
using System.Globalization;
using System.Windows;

namespace AutoTool.Desktop.View.Converters;

/// <summary>
/// 真偽値を反転して返す値コンバーターです。
/// </summary>
public class InvertBooleanConverter : IValueConverter
{
    /// <summary>
    /// バインディング値を `true/false` で反転します。
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }

    /// <summary>
    /// 逆変換でも同じく真偽値反転を行います。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }
}

/// <summary>
/// 実行状態の真偽値を表示用文言へ変換するコンバーターです。
/// </summary>
public class RunningStatusConverter : IValueConverter
{
    /// <summary>
    /// `true/false` を「実行中/停止中」へ変換します。
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
            return isRunning ? "実行中" : "停止中";
        return "不明";
    }

    /// <summary>
    /// この変換は片方向のため逆変換は行いません。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// 真偽値と `Visibility` を相互変換するコンバーターです。
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// `true` は `Visible`、それ以外は `Collapsed` に変換します。
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    /// <summary>
    /// `Visibility` から真偽値へ戻します。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        return false;
    }
}

/// <summary>
/// コレクション件数が 1 件以上ある場合だけ表示します。
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            int count when count > 0 => Visibility.Visible,
            _ => Visibility.Collapsed,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// お気に入りパネルの開閉状態と幅から `GridLength` を計算するコンバーターです。
/// </summary>
public class FavoritePanelWidthMultiConverter : IMultiValueConverter
{
    /// <summary>
    /// 開いているときだけ幅を反映し、閉じているときは 0 幅にします。
    /// </summary>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var isOpen = values.Length > 0 && values[0] is bool open && open;
        var width = values.Length > 1 && values[1] is double panelWidth ? panelWidth : 340d;
        var normalizedWidth = Math.Clamp(width, 240d, 700d);
        return new GridLength(isOpen ? normalizedWidth : 0d, GridUnitType.Pixel);
    }

    /// <summary>
    /// グリッド幅の変更をパネル幅へ戻し、開閉状態は変更しません。
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength && gridLength.IsAbsolute)
        {
            var normalizedWidth = Math.Clamp(gridLength.Value, 240d, 700d);
            return [Binding.DoNothing, normalizedWidth];
        }

        return [Binding.DoNothing, 340d];
    }
}
