using System.Windows;
using System.Windows.Input;

namespace MacroPanels.View;

public partial class KeyPickerWindow : Window
{
    public Key SelectedKey { get; private set; } = Key.None;

    public KeyPickerWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // 修飾キー単体は無視（Ctrl, Alt, Shift, Windowsキー）
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
            e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
            e.Key == Key.LeftShift || e.Key == Key.RightShift ||
            e.Key == Key.LWin || e.Key == Key.RWin ||
            e.Key == Key.System)
        {
            return;
        }

        // システムキーの場合は実際のキーを取得
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        
        SelectedKey = key;
        KeyDisplayText.Text = key.ToString();
        OkButton.IsEnabled = true;
        
        e.Handled = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
