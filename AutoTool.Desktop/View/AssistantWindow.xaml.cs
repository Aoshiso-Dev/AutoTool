using System.ComponentModel;
using System.Windows;
using AutoTool.Desktop.Panels.ViewModel;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop.View;

/// <summary>
/// AI相談を独立したウィンドウとして表示します。
/// </summary>
public partial class AssistantWindow : FluentWindow
{
    public AssistantWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is IAssistantPanelViewModel assistantPanelViewModel)
        {
            assistantPanelViewModel.RequestCancel();
        }

        base.OnClosing(e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
