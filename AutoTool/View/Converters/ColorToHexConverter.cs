using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoTool.View.Converters
{
    public class ColorToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            if (value is System.Drawing.Color d)
            {
                return $"#{d.R:X2}{d.G:X2}{d.B:X2}";
            }

            if (value is System.Windows.Media.Color m)
            {
                return $"#{m.R:X2}{m.G:X2}{m.B:X2}";
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}