using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;
using MacroPanels.View;
using System.Windows.Controls;

namespace MacroPanels.View.Converter
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool boolValue ? (boolValue ? Visibility.Visible : Visibility.Collapsed) : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public sealed class BoolToVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // LINQ使用でより簡潔に
            return values.OfType<bool>().Any(b => b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ブール値を反転するコンバータ
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool boolValue ? !boolValue : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ブール値を文字列に変換するコンバータ
    public sealed class BooleanToTextConverter : IValueConverter
    {
        public string? TrueText { get; set; }
        public string? FalseText { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool boolValue ? (boolValue ? TrueText : FalseText) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // 数値が一致したらTrueを返すコンバータ
    public sealed class NumberToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int lineNumber && values[1] is int executedLineNumber)
            {
                Debug.WriteLine($"LineNumber: {lineNumber}, ExecutedLineNumber: {executedLineNumber}");
                return lineNumber == executedLineNumber;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // 文字列が一致したらVisibleを返すコンバータ
    public sealed class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string stringValue && parameter is string targetString && stringValue == targetString 
                ? Visibility.Visible 
                : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public sealed class KeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Key key ? (key == Key.None ? string.Empty : key.ToString()) : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string keyString && Enum.TryParse<Key>(keyString, out var key) ? key : Key.None;
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
    public sealed class NestLevelToMarginConverter : IValueConverter
    {
        private const int IndentSize = 20; // 定数として定義

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var nestLevel = value is int level ? level : 0;
            return new Thickness(nestLevel * IndentSize, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
