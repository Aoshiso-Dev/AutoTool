using System.Windows;
using System.Windows.Controls;
using AutoTool.Desktop.Panels.ViewModel;

namespace AutoTool.Desktop.Panels.View;

/// <summary>
/// ButtonPanel.xaml の相互作用ロジック
/// </summary>
public partial class ButtonPanel : UserControl
{
    public ButtonPanel()
    {
        InitializeComponent();
    }

    private void AddCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ButtonPanelViewModel viewModel)
        {
            return;
        }

        var commandSelectionWindow = new CommandSelectionWindow(viewModel.ItemTypes, viewModel.SelectedItemType)
        {
            Owner = Window.GetWindow(this)
        };

        if (commandSelectionWindow.ShowDialog() != true || commandSelectionWindow.SelectedCommand is null)
        {
            return;
        }

        viewModel.SelectedItemType = commandSelectionWindow.SelectedCommand;
        viewModel.Add();
    }
}
