using System.Windows;
using Wpf.Ui.Controls;

namespace MacroPanels.View;

public partial class EditPanelWindow : FluentWindow
{
    public EditPanelWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// EditPanelÇÃDataContextÇê›íË
    /// </summary>
    public void SetEditPanelDataContext(object dataContext)
    {
        EditPanelContent.DataContext = dataContext;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
