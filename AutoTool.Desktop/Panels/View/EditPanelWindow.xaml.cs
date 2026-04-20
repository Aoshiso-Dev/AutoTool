using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfNumberBox = Wpf.Ui.Controls.NumberBox;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop.Panels.View;

/// <summary>
/// コマンド編集パネルの表示と入力イベントを処理し、編集対象に応じた UI 更新を行います。
/// </summary>
public partial class EditPanelWindow : FluentWindow
{
    public EditPanelWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// EditPanel の `DataContext` を設定します。
    /// </summary>
    public void SetEditPanelDataContext(object dataContext)
    {
        EditPanelContent.DataContext = dataContext;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        CommitPendingEdits();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CommitPendingEdits()
    {
        // フォーカス中コントロールの編集中テキストを確定させる
        MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        UpdateBindingsRecursive(this);
    }

    private static void UpdateBindingsRecursive(DependencyObject root)
    {
        Action update = root switch
        {
            WpfNumberBox numberBox
                => () => BindingOperations.GetBindingExpression(numberBox, WpfNumberBox.ValueProperty)?.UpdateSource(),
            System.Windows.Controls.TextBox textBox
                => () => BindingOperations.GetBindingExpression(textBox, System.Windows.Controls.TextBox.TextProperty)?.UpdateSource(),
            System.Windows.Controls.ComboBox comboBox
                => () =>
                {
                    BindingOperations.GetBindingExpression(comboBox, System.Windows.Controls.ComboBox.SelectedItemProperty)?.UpdateSource();
                    BindingOperations.GetBindingExpression(comboBox, System.Windows.Controls.ComboBox.SelectedValueProperty)?.UpdateSource();
                    BindingOperations.GetBindingExpression(comboBox, System.Windows.Controls.ComboBox.TextProperty)?.UpdateSource();
                },
            System.Windows.Controls.Slider slider
                => () => BindingOperations.GetBindingExpression(slider, System.Windows.Controls.Slider.ValueProperty)?.UpdateSource(),
            ToggleButton toggleButton
                => () => BindingOperations.GetBindingExpression(toggleButton, ToggleButton.IsCheckedProperty)?.UpdateSource(),
            _ => static () => { }
        };

        update();

        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            UpdateBindingsRecursive(VisualTreeHelper.GetChild(root, i));
        }
    }
}
