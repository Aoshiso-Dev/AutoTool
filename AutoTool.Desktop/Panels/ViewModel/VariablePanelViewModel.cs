using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
using AutoTool.Commands.Services;
using AutoTool.Desktop.Panels.ViewModel.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// 実行中に更新される変数を一覧表示する ViewModel です。
/// </summary>
public partial class VariablePanelViewModel : ObservableObject, IVariablePanelViewModel, IDisposable
{
    private readonly IVariableStore _variableStore;
    private readonly IObservableVariableStore? _observableVariableStore;
    private readonly ObservableCollection<VariableEntry> _variables = [];
    private readonly ICollectionView _filteredVariables;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public event Action<string>? StatusMessageRequested;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<VariableEntry> Variables => _variables;
    public ICollectionView FilteredVariables => _filteredVariables;

    public VariablePanelViewModel(IVariableStore variableStore, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(variableStore);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _variableStore = variableStore;
        _observableVariableStore = variableStore as IObservableVariableStore;
        _timeProvider = timeProvider;
        _filteredVariables = CollectionViewSource.GetDefaultView(_variables);
        _filteredVariables.Filter = FilterVariable;

        if (_observableVariableStore is not null)
        {
            _observableVariableStore.Changed += OnVariableStoreChanged;
        }

        Refresh();
    }

    public void SetRunningState(bool isRunning)
    {
        IsRunning = isRunning;
        Refresh();
    }

    public void Prepare() => Refresh();

    public void Refresh()
    {
        var snapshot = _observableVariableStore?.GetSnapshot() ?? new Dictionary<string, string>();
        var updatedAt = _timeProvider.GetLocalNow().ToString("HH:mm:ss");
        RunOnUiThread(() =>
        {
            _variables.Clear();
            foreach (var variable in snapshot.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                _variables.Add(CreateEntry(variable.Key, variable.Value, updatedAt));
            }
        });
    }

    [RelayCommand]
    private void CopyVariables()
    {
        var variables = _filteredVariables.Cast<VariableEntry>().ToList();
        if (variables.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder(variables.Count * 32);
        foreach (var variable in variables)
        {
            builder.Append(variable.Name)
                .Append('\t')
                .Append(variable.Value)
                .Append('\t')
                .Append(variable.UpdatedAt)
                .AppendLine();
        }

        try
        {
            RunOnUiThread(() => System.Windows.Clipboard.SetText(builder.ToString()));
        }
        catch (Exception ex)
        {
            StatusMessageRequested?.Invoke($"変数のコピーに失敗しました。{ex.Message}");
            return;
        }

        StatusMessageRequested?.Invoke($"変数をコピーしました（{variables.Count}件）。");
    }

    [RelayCommand]
    private void RefreshVariables()
    {
        Refresh();
        StatusMessageRequested?.Invoke($"変数を更新しました（{_variables.Count}件）。");
    }

    partial void OnSearchTextChanged(string value)
    {
        _filteredVariables.Refresh();
    }

    private bool FilterVariable(object obj)
    {
        if (obj is not VariableEntry variable)
        {
            return false;
        }

        var keyword = SearchText?.Trim();
        return string.IsNullOrWhiteSpace(keyword)
            || variable.SearchableText.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void OnVariableStoreChanged(object? sender, VariableStoreChangedEventArgs e)
    {
        var updatedAt = _timeProvider.GetLocalNow().ToString("HH:mm:ss");
        RunOnUiThread(() =>
        {
            if (e.IsClear)
            {
                _variables.Clear();
                return;
            }

            if (string.IsNullOrWhiteSpace(e.Name))
            {
                return;
            }

            var existing = _variables.FirstOrDefault(x => string.Equals(x.Name, e.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                _variables.Remove(existing);
            }

            _variables.Add(CreateEntry(e.Name, e.Value ?? string.Empty, updatedAt));
            SortVariables();
        });
    }

    private void SortVariables()
    {
        var ordered = _variables.OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        _variables.Clear();
        foreach (var variable in ordered)
        {
            _variables.Add(variable);
        }
    }

    private static VariableEntry CreateEntry(string name, string value, string updatedAt)
    {
        return new VariableEntry
        {
            Name = name,
            Value = value,
            UpdatedAt = updatedAt
        };
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_observableVariableStore is not null)
        {
            _observableVariableStore.Changed -= OnVariableStoreChanged;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
