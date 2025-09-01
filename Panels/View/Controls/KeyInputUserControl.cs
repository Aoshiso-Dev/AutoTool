using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MacroPanels.View.Controls
{
    /// <summary>
    /// キー入力専用のUserControl
    /// </summary>
    public partial class KeyInputUserControl : UserControl
    {
        public static readonly DependencyProperty KeyValueProperty =
            DependencyProperty.Register(
                nameof(KeyValue),
                typeof(Key),
                typeof(KeyInputUserControl),
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

        public KeyInputUserControl()
        {
            InitializeComponent();
            UpdateDisplayText();
            System.Diagnostics.Debug.WriteLine($"KeyInputUserControl constructor - KeyValue: {KeyValue}");
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"KeyInputUserControl OnPreviewKeyDown - Key: {e.Key}");
            
            // システムキーの場合は無視
            if (e.Key == Key.System)
            {
                e.Handled = true;
                return;
            }

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
            System.Diagnostics.Debug.WriteLine($"KeyInputUserControl KeyValue set to: {KeyValue}");
            e.Handled = true;
        }

        private static void OnKeyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeyInputUserControl control)
            {
                System.Diagnostics.Debug.WriteLine($"KeyInputUserControl OnKeyValueChanged - Old: {e.OldValue}, New: {e.NewValue}");
                control.UpdateDisplayText();
            }
        }

        private void UpdateDisplayText()
        {
            if (KeyTextBox != null)
            {
                var displayText = KeyValue == Key.None ? "" : GetKeyDisplayName(KeyValue);
                KeyTextBox.Text = displayText;
                System.Diagnostics.Debug.WriteLine($"KeyInputUserControl UpdateDisplayText - KeyValue: {KeyValue}, Text: {displayText}");
            }
        }

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
                _ => key.ToString()
            };
        }

        private void InitializeComponent()
        {
            KeyTextBox = new TextBox
            {
                IsReadOnly = true,
                Focusable = true,
                Background = System.Windows.Media.Brushes.White,
                Foreground = System.Windows.Media.Brushes.Black,
                BorderBrush = System.Windows.Media.Brushes.Gray
            };

            KeyTextBox.GotFocus += (s, e) => 
            {
                KeyTextBox.SelectAll();
                System.Diagnostics.Debug.WriteLine("KeyInputUserControl TextBox got focus");
            };

            Content = KeyTextBox;
            Focusable = true;
        }

        private TextBox KeyTextBox { get; set; }
    }
}