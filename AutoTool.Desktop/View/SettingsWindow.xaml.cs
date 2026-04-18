using Wpf.Ui.Controls;

namespace AutoTool.Desktop.View;

public partial class SettingsWindow : FluentWindow
{
    public bool RestorePreviousSession { get; private set; }

    public SettingsWindow(bool restorePreviousSession)
    {
        InitializeComponent();
        RestorePreviousSession = restorePreviousSession;
        RestorePreviousSessionCheckBox.IsChecked = restorePreviousSession;
    }

    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        RestorePreviousSession = RestorePreviousSessionCheckBox.IsChecked ?? true;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
