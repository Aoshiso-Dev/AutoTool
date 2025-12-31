using CommunityToolkit.Mvvm.ComponentModel;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using System.Windows.Data;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.ViewModel;

public partial class ListPanelViewModel : ObservableObject, IListPanelViewModel
{
    private readonly ICommandRegistry _commandRegistry;
    private object? _commandHistory;

    // イベント
    public event Action<ICommandListItem?>? SelectedItemChanged;

    #region Properties
    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private CommandList _commandList = new();

    private int _selectedLineNumber;
    public int SelectedLineNumber
    {
        get => _selectedLineNumber;
        set
        {
            if (SetProperty(ref _selectedLineNumber, value))
            {
                OnSelectedLineNumberChanged();
            }
        }
    }

    public ICommandListItem? SelectedItem
    {
        get => CommandList.Items.FirstOrDefault(x => x.IsSelected);
        set
        {
            if (value == null) return;

            var existingItem = CommandList.Items.FirstOrDefault(x => x.IsSelected);
            if (existingItem != null)
            {
                var index = CommandList.Items.IndexOf(existingItem);
                CommandList.Override(index, value);
                // Refresh()を削除 - ObservableCollectionが自動的に通知する
            }
        }
    }

    private int _executedLineNumber;
    public int ExecutedLineNumber
    {
        get => _executedLineNumber;
        set
        {
            SetProperty(ref _executedLineNumber, value);
            OnExecutedLineNumberChanged();
        }
    }
    #endregion

    public ListPanelViewModel(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    /// <summary>
    /// CommandHistoryManagerを設定
    /// </summary>
    public void SetCommandHistory(object commandHistory)
    {
        _commandHistory = commandHistory;
    }

    #region OnChanged
    private void OnSelectedLineNumberChanged()
    {
        foreach (var item in CommandList.Items)
        {
            item.IsSelected = false;
        }

        var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
        if (existingItem != null)
        {
            existingItem.IsSelected = true;
            SelectedItemChanged?.Invoke(existingItem);
        }
        // Refresh()を削除 - IsSelectedはINotifyPropertyChangedで通知される
    }

    private void OnExecutedLineNumberChanged()
    {
        foreach (var item in CommandList.Items)
        {
            item.IsRunning = false;
        }
        
        var cmd = CommandList.Items.FirstOrDefault(x => x.LineNumber == ExecutedLineNumber);
        if (cmd != null)
        {
            cmd.IsRunning = true;
            // Refresh()を削除 - IsRunningはINotifyPropertyChangedで通知される
        }
    }
    #endregion

    #region ListInteraction
    public void Refresh()
    {
        // 必要な場合のみ呼び出す（Load後など）
        CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
    }

    public void Add(string itemType)
    {
        var item = _commandRegistry.CreateCommandItem(itemType);

        if (item != null)
        {
            item.ItemType = itemType;

            if (CommandList.Items.Count != 0 && SelectedLineNumber >= 0)
            {
                CommandList.Insert(SelectedLineNumber + 1, item);
            }
            else
            {
                CommandList.Add(item);
            }

            SelectedLineNumber = CommandList.Items.IndexOf(item);
            // Refresh()を削除 - ObservableCollectionのInsert/Addで自動通知
        }
    }

    /// <summary>
    /// 指定位置にアイテムを挿入（Undo/Redo用）
    /// </summary>
    public void InsertAt(int index, ICommandListItem item)
    {
        if (index < 0) index = 0;
        if (index > CommandList.Items.Count) index = CommandList.Items.Count;

        CommandList.Insert(index, item);
        SelectedLineNumber = index;
        // Refresh()を削除
    }

    /// <summary>
    /// 指定位置のアイテムを削除（Undo/Redo用）
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index >= 0 && index < CommandList.Items.Count)
        {
            CommandList.RemoveAt(index);
            
            if (CommandList.Items.Count == 0)
            {
                SelectedLineNumber = 0;
            }
            else if (index >= CommandList.Items.Count)
            {
                SelectedLineNumber = CommandList.Items.Count - 1;
            }
            else
            {
                SelectedLineNumber = index;
            }
            // Refresh()を削除
        }
    }

    /// <summary>
    /// 指定位置のアイテムを置換（Undo/Redo用）
    /// </summary>
    public void ReplaceAt(int index, ICommandListItem item)
    {
        if (index >= 0 && index < CommandList.Items.Count)
        {
            CommandList.Override(index, item);
            SelectedLineNumber = index;
            // Refresh()を削除
        }
    }

    /// <summary>
    /// アイテムを移動（Undo/Redo用）
    /// </summary>
    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex >= 0 && fromIndex < CommandList.Items.Count &&
            toIndex >= 0 && toIndex < CommandList.Items.Count &&
            fromIndex != toIndex)
        {
            CommandList.Move(fromIndex, toIndex);
            SelectedLineNumber = toIndex;
            // Refresh()を削除
        }
    }

    /// <summary>
    /// アイテムを追加（Undo/Redo用）
    /// </summary>
    public void AddItem(ICommandListItem item)
    {
        CommandList.Add(item);
        // Refresh()を削除
    }

    public void Up()
    {
        if (SelectedLineNumber == 0) return;

        var selectedBak = SelectedLineNumber;
        CommandList.Move(SelectedLineNumber, SelectedLineNumber - 1);
        SelectedLineNumber = selectedBak - 1;
    }

    public void Down()
    {
        if (SelectedLineNumber == CommandList.Items.Count - 1) return;

        var selectedBak = SelectedLineNumber;
        CommandList.Move(SelectedLineNumber, SelectedLineNumber + 1);
        SelectedLineNumber = selectedBak + 1;
    }

    public void Delete()
    {
        if (SelectedItem == null) return;

        var index = CommandList.Items.IndexOf(SelectedItem);
        CommandList.Remove(SelectedItem);

        if (CommandList.Items.Count == 0)
        {
            SelectedLineNumber = 0;
        }
        else if (index == CommandList.Items.Count)
        {
            SelectedLineNumber = index - 1;
        }
        else
        {
            SelectedLineNumber = index;
        }
    }

    public void Clear()
    {
        CommandList.Clear();
        SelectedLineNumber = 0;
    }

    public void Save(string filePath = "")
    {
        CommandList.Save(filePath);
    }

    public void Load(string filePath = "")
    {
        CommandList.Load(filePath);
        SelectedLineNumber = 0;
        SelectedItem = CommandList.Items.FirstOrDefault();

        _commandRegistry.Initialize();
        // Load後はRefresh()が必要（全データが入れ替わるため）
        CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        
        foreach (var item in CommandList.Items)
        {
            var currentValue = item.ItemType;
            item.ItemType = currentValue;
        }
    }
    #endregion

    #region Call from MainWindowViewModel
    public int GetCount() => CommandList.Items.Count;

    public ICommandListItem? GetRunningItem() => CommandList.Items.FirstOrDefault(x => x.IsRunning);

    public ICommandListItem? GetItem(int lineNumber) => CommandList.Items.FirstOrDefault(x => x.LineNumber == lineNumber);

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;

    public void SetSelectedItem(ICommandListItem? item) => SelectedItem = item;

    public void SetSelectedLineNumber(int lineNumber) => SelectedLineNumber = lineNumber;

    public void Prepare()
    {
        foreach (var item in CommandList.Items)
        {
            item.IsRunning = false;
            item.Progress = 0;
        }
        // Prepare後はRefresh()が必要（複数アイテムの状態が変わるため）
        CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
    }
    #endregion
}
