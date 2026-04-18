using AutoTool.Desktop.Model;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop.View;

public partial class SettingsWindow : FluentWindow
{
    public bool RestorePreviousSession { get; private set; }
    public WindowSizePreset SelectedWindowSizePreset { get; private set; }

    public SettingsWindow(bool restorePreviousSession, WindowSizePreset windowSizePreset)
    {
        InitializeComponent();
        RestorePreviousSession = restorePreviousSession;
        SelectedWindowSizePreset = windowSizePreset;
        RestorePreviousSessionCheckBox.IsChecked = restorePreviousSession;
        WindowSizePresetComboBox.SelectedIndex = windowSizePreset switch
        {
            WindowSizePreset.Compact => 0,
            WindowSizePreset.Standard => 1,
            WindowSizePreset.Large => 2,
            _ => 1
        };
    }

    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        RestorePreviousSession = RestorePreviousSessionCheckBox.IsChecked ?? true;
        SelectedWindowSizePreset = WindowSizePresetComboBox.SelectedIndex switch
        {
            0 => WindowSizePreset.Compact,
            2 => WindowSizePreset.Large,
            _ => WindowSizePreset.Standard
        };

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
