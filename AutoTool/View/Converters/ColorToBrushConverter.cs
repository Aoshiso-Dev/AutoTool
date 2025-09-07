using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Drawing;

namespace AutoTool.View.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return System.Windows.Media.Brushes.Transparent;

            // Support System.Drawing.Color and System.Windows.Media.Color
            if (value is System.Drawing.Color d)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(d.A, d.R, d.G, d.B));
            }
            if (value is System.Windows.Media.Color m)
            {
                return new SolidColorBrush(m);
            }

            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}