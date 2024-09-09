using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;
using Panels.View;
using System.Windows.Controls;

namespace Panels.View.Converter
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

    public class KeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Key key)
            {
                if (key == Key.None)
                {
                    return string.Empty;
                }

                return key.ToString();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string keyString && Enum.TryParse(typeof(Key), keyString, out var key))
            {
                return key;
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



    // NestLevelをマージンに変換するコンバータ
    public class NestLevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int nestLevel = (int)value;
            int indentSize = 20; // インデントの幅
            return new Thickness(nestLevel * indentSize, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
