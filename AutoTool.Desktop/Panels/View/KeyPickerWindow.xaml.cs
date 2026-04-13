using System.Windows;
using System.Windows.Input;

namespace AutoTool.Panels.View;

public partial class KeyPickerWindow : Window
{
    public Key SelectedKey { get; private set; } = Key.None;
    public bool SelectedCtrl { get; private set; }
    public bool SelectedAlt { get; private set; }
    public bool SelectedShift { get; private set; }

    public KeyPickerWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Focus();
            Keyboard.Focus(this);
        };
        Activated += (_, _) =>
        {
            Focus();
            Keyboard.Focus(this);
        };
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

        var modifiers = Keyboard.Modifiers;
        SelectedCtrl = modifiers.HasFlag(ModifierKeys.Control);
        SelectedAlt = modifiers.HasFlag(ModifierKeys.Alt);
        SelectedShift = modifiers.HasFlag(ModifierKeys.Shift);
        SelectedKey = key;
        KeyDisplayText.Text = BuildDisplayText(key, SelectedCtrl, SelectedAlt, SelectedShift);
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

    private static string BuildDisplayText(Key key, bool ctrl, bool alt, bool shift)
    {
        var keys = new List<string>();
        if (ctrl) keys.Add("Ctrl");
        if (alt) keys.Add("Alt");
        if (shift) keys.Add("Shift");
        keys.Add(key.ToString());
        return string.Join(" + ", keys);
    }
}

