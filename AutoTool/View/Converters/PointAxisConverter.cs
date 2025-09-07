using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoTool.View.Converters
{
    /// <summary>
    /// Point(CurrentValue) から X または Y を取り出し文字列化するコンバータ。
    /// ConverterParameter に "X" or "Y" を指定。
    /// 戻りは文字列。未取得時は "0"。
    /// 逆変換は未使用（TwoWay 更新はコードビハインドの TextChanged で処理）。
    /// </summary>
    public class PointAxisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is System.Windows.Point p && parameter is string axis)
                {
                    return axis == "Y" ? ((int)p.Y).ToString() : ((int)p.X).ToString();
                }
            }
            catch { }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 逆方向は使用しない（TextChangedで MousePosition を更新）
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
