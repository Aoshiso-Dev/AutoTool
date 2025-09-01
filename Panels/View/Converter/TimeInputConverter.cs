using System;
using System.Globalization;
using System.Windows.Data;

namespace MacroPanels.View.Converter
{
    /// <summary>
    /// ���l���͗p�̃R���o�[�^�[�i���̒l��0�ɐ����j
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
                    return Math.Max(0, result); // ���̒l��0�ɐ���
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// ���E�b�p�̃R���o�[�^�[�i0-59�͈̔͂ɐ����j
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
                    return Math.Max(0, Math.Min(MaxValue, result)); // 0-MaxValue�͈̔͂ɐ���
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// �~���b�p�̃R���o�[�^�[�i0-999�͈̔͂ɐ����j
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
                    return Math.Max(0, Math.Min(999, result)); // 0-999�͈̔͂ɐ���
                }
            }
            return 0;
        }
    }
}