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

public class InvertBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }
}

public class RunningStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
            return isRunning ? "実行中" : "停止中";
        return "不明";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
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
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        return false;
    }
}

public class FavoritePanelWidthMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var isOpen = values.Length > 0 && values[0] is bool open && open;
        var width = values.Length > 1 && values[1] is double panelWidth ? panelWidth : 340d;
        var normalizedWidth = Math.Clamp(width, 240d, 700d);
        return new GridLength(isOpen ? normalizedWidth : 0d, GridUnitType.Pixel);
    }

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
