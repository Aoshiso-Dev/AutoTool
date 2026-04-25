using System.Collections.Specialized;
using System.Windows.Controls;
using AutoTool.Desktop.Panels.ViewModel;

namespace AutoTool.Desktop.Panels.View;

/// <summary>
/// 変数一覧の表示更新を処理し、直近の変更を追いやすくします。
/// </summary>
public partial class VariablePanel : UserControl
{
    private VariablePanelViewModel? _viewModel;

    public VariablePanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        DetachFromViewModel();
        _viewModel = DataContext as VariablePanelViewModel;
        AttachToViewModel();
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        DetachFromViewModel();
    }

    private void AttachToViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.Variables.CollectionChanged += OnVariablesChanged;
    }

    private void DetachFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.Variables.CollectionChanged -= OnVariablesChanged;
        _viewModel = null;
    }

    private void OnVariablesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is null || e.NewItems.Count == 0)
        {
            return;
        }

        var latest = e.NewItems[^1];
        Dispatcher.BeginInvoke(() => VariableListBox.ScrollIntoView(latest));
    }
}
