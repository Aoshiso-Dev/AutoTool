using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MacroPanels.View.Controls
{
    /// <summary>
    /// キー入力専用のTextBox
    /// </summary>
    public class KeyInputBox : TextBox
    {
        public static readonly DependencyProperty KeyValueProperty =
            DependencyProperty.Register(
                nameof(KeyValue),
                typeof(Key),
                typeof(KeyInputBox),
                new FrameworkPropertyMetadata(
                    Key.None,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnKeyValueChanged));

        /// <summary>
        /// バインディング用のKeyプロパティ
        /// </summary>
        public Key KeyValue
        {
            get => (Key)GetValue(KeyValueProperty);
            set => SetValue(KeyValueProperty, value);
        }

        static KeyInputBox()
        {
            // TextBoxの既定のスタイルを継承するように修正
            DefaultStyleKeyProperty.OverrideMetadata(typeof(KeyInputBox), new FrameworkPropertyMetadata(typeof(TextBox)));
        }

        public KeyInputBox()
        {
            IsReadOnly = true; // 通常の文字入力を無効化
            Focusable = true;
            
            // 初期表示を設定
            UpdateDisplayText();
            
            // デバッグ用：初期値を設定
            System.Diagnostics.Debug.WriteLine($"KeyInputBox constructor called - KeyValue: {KeyValue}");
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"KeyInputBox OnPreviewKeyDown - Key: {e.Key}");
            
            // システムキーの場合は無視
            if (e.Key == Key.System)
            {
                e.Handled = true;
                return;
            }

            // 特殊キーの処理
            var key = e.Key;
            
            // Alt+F4などのシステムキーは除外
            if (key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                e.Handled = true;
                return;
            }

            // Tabキーは通常の動作を維持
            if (key == Key.Tab)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            // Escapeキーでクリア
            if (key == Key.Escape)
            {
                KeyValue = Key.None;
                e.Handled = true;
                return;
            }

            // その他のキーを設定
            KeyValue = key;
            System.Diagnostics.Debug.WriteLine($"KeyInputBox KeyValue set to: {KeyValue}");
            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            SelectAll(); // フォーカス取得時にテキスト全選択
            System.Diagnostics.Debug.WriteLine("KeyInputBox got focus");
        }

        private static void OnKeyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeyInputBox keyInputBox)
            {
                System.Diagnostics.Debug.WriteLine($"KeyInputBox OnKeyValueChanged - Old: {e.OldValue}, New: {e.NewValue}");
                keyInputBox.UpdateDisplayText();
            }
        }

        private void UpdateDisplayText()
        {
            var displayText = KeyValue == Key.None ? "" : GetKeyDisplayName(KeyValue);
            Text = displayText;
            System.Diagnostics.Debug.WriteLine($"KeyInputBox UpdateDisplayText - KeyValue: {KeyValue}, Text: {displayText}");
        }

        /// <summary>
        /// キーの表示名を取得
        /// </summary>
        private static string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                Key.None => "",
                Key.Space => "Space",
                Key.Return => "Enter",
                Key.Back => "Backspace",
                Key.Delete => "Delete",
                Key.Insert => "Insert",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.Up => "↑",
                Key.Down => "↓",
                Key.Left => "←",
                Key.Right => "→",
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.F11 => "F11",
                Key.F12 => "F12",
                Key.NumPad0 => "Num0",
                Key.NumPad1 => "Num1",
                Key.NumPad2 => "Num2",
                Key.NumPad3 => "Num3",
                Key.NumPad4 => "Num4",
                Key.NumPad5 => "Num5",
                Key.NumPad6 => "Num6",
                Key.NumPad7 => "Num7",
                Key.NumPad8 => "Num8",
                Key.NumPad9 => "Num9",
                Key.Multiply => "Num*",
                Key.Add => "Num+",
                Key.Subtract => "Num-",
                Key.Divide => "Num/",
                Key.Decimal => "Num.",
                _ => key.ToString()
            };
        }
    }
}