using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoTool.Desktop.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // parameterÇ™"Invert"ÇÃèÍçáÇÕãtïœä∑
            bool shouldInvert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
            
            if (shouldInvert)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool shouldInvert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
            
            if (shouldInvert)
            {
                return visibility != Visibility.Visible;
            }
            else
            {
                return visibility == Visibility.Visible;
            }
        }
        return false;
    }
}