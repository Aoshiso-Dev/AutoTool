using System;
using System.Globalization;
using System.Windows.Data;
using Panels;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;
using Panels.View;
using System.Windows.Controls;
using Panels.Command.Factory;

namespace Panels.Converter
{
    // ブール値を文字列に変換するコンバータ
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

    // 文字列が一致したらVisibleを返すコンバータ
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string targetString)
            {
                return stringValue == targetString ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Keyを文字列に変換するコンバータ
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

    // Keyを文字列に変換するコンバータ
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
