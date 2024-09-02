using System;
using System.Globalization;
using System.Windows.Data;
using Panels;
using System.Windows.Input;
using Panels.Define;
using System.Windows;
using System.Diagnostics;
using Panels.View;
using System.Windows.Controls;

namespace Panels.Converter
{
    public class BooleanToTextConverter : IValueConverter
    {
        public string? TrueText { get; set; }
        public string? FalseText { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueText : FalseText;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // コマンドタイプが一致したらVisibleを返すコンバータ
    public class CommandTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CommandType commandType && parameter is string targetTypeString)
            {
                if (Enum.TryParse(typeof(CommandType), targetTypeString, out var targetType_))
                {
                    return commandType.Equals(targetType_) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ConditionTypeが一致したらVisibleを返すコンバータ
    public class ConditionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConditionType conditionType && parameter is string targetTypeString)
            {
                if (Enum.TryParse(typeof(ConditionType), targetTypeString, out var targetType_))
                {
                    return conditionType.Equals(targetType_) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
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
            if (value is Key key)
            {
                return key.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string keyString && Enum.TryParse(typeof(Key), keyString, out var result))
            {
                return result;
            }
            return Key.None;
        }
    }

    public static class KeyInputBehavior
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached(
                "Key",
                typeof(Key),
                typeof(KeyInputBehavior),
                new FrameworkPropertyMetadata(Key.None, OnKeyChanged));

        public static Key GetKey(DependencyObject obj) => (Key)obj.GetValue(KeyProperty);
        public static void SetKey(DependencyObject obj, Key value) => obj.SetValue(KeyProperty, value);

        private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.PreviewKeyDown -= OnPreviewKeyDown; // 既存のハンドラを削除
                textBox.PreviewKeyDown += OnPreviewKeyDown; // 新しいハンドラを追加
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SetKey(textBox, e.Key);
                textBox.Text = e.Key.ToString(); // テキストボックスにキー名を表示
                e.Handled = true;
            }
        }
    }
}
