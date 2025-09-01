using System;
using System.Globalization;
using System.Windows.Data;

namespace MacroPanels.View.Converter
{
    /// <summary>
    /// 数値入力用のコンバーター（負の値を0に制限）
    /// </summary>
    public class NonNegativeIntegerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue.ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, out int result))
                {
                    return Math.Max(0, result); // 負の値は0に制限
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// 分・秒用のコンバーター（0-59の範囲に制限）
    /// </summary>
    public class TimeComponentConverter : IValueConverter
    {
        public int MaxValue { get; set; } = 59;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue.ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, out int result))
                {
                    return Math.Max(0, Math.Min(MaxValue, result)); // 0-MaxValueの範囲に制限
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// ミリ秒用のコンバーター（0-999の範囲に制限）
    /// </summary>
    public class MillisecondConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue.ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, out int result))
                {
                    return Math.Max(0, Math.Min(999, result)); // 0-999の範囲に制限
                }
            }
            return 0;
        }
    }
}