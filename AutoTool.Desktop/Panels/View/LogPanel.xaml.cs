using System.Collections.Specialized;
using System.Windows.Controls;
using AutoTool.Panels.ViewModel;

namespace AutoTool.Panels.View;

public partial class LogPanel : UserControl
{
    private LogPanelViewModel? _viewModel;

    public LogPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        DetachFromViewModel();
        _viewModel = DataContext as LogPanelViewModel;
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

        _viewModel.LogEntries.CollectionChanged += OnLogEntriesChanged;
    }

    private void DetachFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.LogEntries.CollectionChanged -= OnLogEntriesChanged;
        _viewModel = null;
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is null || e.NewItems.Count == 0)
        {
            return;
        }

        var latest = e.NewItems[^1];
        Dispatcher.BeginInvoke(() => LogListBox.ScrollIntoView(latest));
    }
}

