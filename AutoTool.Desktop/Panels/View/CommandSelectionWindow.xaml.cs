using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Desktop.Panels.ViewModel.Shared;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop.Panels.View;

/// <summary>
/// コマンド追加時に表示する選択ウィンドウです。
/// </summary>
public partial class CommandSelectionWindow : FluentWindow, INotifyPropertyChanged
{
    private readonly IReadOnlyList<CommandDisplayItem> _allCommands;
    private string _searchText = string.Empty;
    private CommandCategoryFilterItem? _selectedCategory;
    private CommandDisplayItem? _selectedCommand;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICollectionView CommandItemsView { get; }
    public ObservableCollection<CommandCategoryFilterItem> CategoryItems { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            RefreshCommandItemsView();
        }
    }

    public CommandCategoryFilterItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (ReferenceEquals(_selectedCategory, value))
            {
                return;
            }

            _selectedCategory = value;
            OnPropertyChanged();
            RefreshCommandItemsView();
        }
    }

    public CommandDisplayItem? SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            if (ReferenceEquals(_selectedCommand, value))
            {
                return;
            }

            _selectedCommand = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanConfirm));
            OnPropertyChanged(nameof(SelectedDescription));
        }
    }

    public bool CanConfirm => SelectedCommand is not null;

    public string SelectedDescription =>
        string.IsNullOrWhiteSpace(SelectedCommand?.Description)
            ? "コマンドを選択すると説明が表示されます。"
            : SelectedCommand.Description;

    public CommandSelectionWindow(IEnumerable<CommandDisplayItem> commands, CommandDisplayItem? selectedCommand = null)
    {
        ArgumentNullException.ThrowIfNull(commands);
        InitializeComponent();

        _allCommands = commands
            .Where(IsSelectableCommand)
            .OrderBy(x => x.DisplayPriority)
            .ThenBy(x => x.DisplaySubPriority)
            .ThenBy(x => x.DisplayName, StringComparer.Ordinal)
            .ToArray();
        CategoryItems = new ObservableCollection<CommandCategoryFilterItem>(CreateCategoryItems(_allCommands));
        _selectedCategory = CategoryItems.FirstOrDefault();
        CommandItemsView = CollectionViewSource.GetDefaultView(_allCommands);
        CommandItemsView.Filter = FilterCommandItem;

        DataContext = this;
        SelectedCommand = selectedCommand ?? _allCommands.FirstOrDefault();
    }

    private bool FilterCommandItem(object obj)
    {
        if (obj is not CommandDisplayItem item)
        {
            return false;
        }

        if (SelectedCategory is { IsAll: false }
            && !string.Equals(item.Category, SelectedCategory.Name, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return item.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.TypeName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshCommandItemsView()
    {
        CommandItemsView.Refresh();
        OnPropertyChanged(nameof(CommandItemsView));

        if (SelectedCommand is null || FilterCommandItem(SelectedCommand))
        {
            return;
        }

        SelectedCommand = CommandItemsView.Cast<CommandDisplayItem>().FirstOrDefault();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!CanConfirm)
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CommandListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!CanConfirm)
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static bool IsSelectableCommand(CommandDisplayItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.TypeName is not (CommandTypeNames.IfEnd or CommandTypeNames.LoopEnd or CommandTypeNames.RetryEnd);
    }

    private static IEnumerable<CommandCategoryFilterItem> CreateCategoryItems(IReadOnlyCollection<CommandDisplayItem> commands)
    {
        yield return new CommandCategoryFilterItem("すべて", commands.Count, true);

        foreach (var group in commands
            .GroupBy(x => x.Category)
            .OrderBy(x => x.Min(item => item.DisplayPriority))
            .ThenBy(x => x.Key, StringComparer.Ordinal))
        {
            yield return new CommandCategoryFilterItem(group.Key, group.Count(), false);
        }
    }

    public sealed record CommandCategoryFilterItem(string Name, int Count, bool IsAll);
}
