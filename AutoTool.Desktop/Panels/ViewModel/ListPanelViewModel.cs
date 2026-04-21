﻿using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using AutoTool.Application.Files;
using AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// 画面状態とユーザー操作を管理する ViewModel です。
/// </summary>
public partial class ListPanelViewModel : ObservableObject, IListPanelViewModel
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly CommandListFileUseCase _commandListFileUseCase;
    private readonly List<ICommandListItem> _selectedItems = [];
    private readonly HashSet<ICommandListItem> _collapsedBlockStarts = [];
    private bool _isPropagatingEnableState;

    // イベント
    public event Action<ICommandListItem?>? SelectedItemChanged;
    public event Action<ICommandListItem?>? ItemDoubleClicked;
    public event Action? InteractionRequested;
    public event Action<int, int>? MoveItemRequested;
    public event Action? DeleteRequested;

    #region Properties
    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isAllSelectedVisual;

    [ObservableProperty]
    private CommandList _commandList;

    [ObservableProperty]
    private IReadOnlyList<ICommandListItem> _validationErrorItems = [];

    [ObservableProperty]
    private int _selectedLineNumber;

    [ObservableProperty]
    private int _collapsedStateVersion;

    [ObservableProperty]
    private int _enableVisualStateVersion;

    public ICommandListItem? SelectedItem
    {
        get => _selectedItems.FirstOrDefault();
        set
        {
            if (value is null)
            {
                if (SelectedLineNumber != -1)
                {
                    SelectedLineNumber = -1;
                }
                else
                {
                    foreach (var item in CommandList.Items)
                    {
                        item.IsSelected = false;
                    }
                    SelectedItemChanged?.Invoke(null);
                }
                return;
            }

            var index = CommandList.Items.IndexOf(value);
            if (index < 0 && value.LineNumber > 0)
            {
                index = CommandList.Items.ToList().FindIndex(x => x.LineNumber == value.LineNumber);
            }

            if (index >= 0)
            {
                if (!ReferenceEquals(CommandList.Items[index], value))
                {
                    CommandList.Override(index, value);
                }

                if (SelectedLineNumber == index)
                {
                    OnSelectedLineNumberChanged();
                }
                else
                {
                    SelectedLineNumber = index;
                }
                return;
            }

            if (SelectedLineNumber >= 0 && SelectedLineNumber < CommandList.Items.Count)
            {
                CommandList.Override(SelectedLineNumber, value);
                OnSelectedLineNumberChanged();
            }
        }
    }

    [ObservableProperty]
    private int _executedLineNumber;
    #endregion

    public ListPanelViewModel(
        ICommandRegistry commandRegistry,
        CommandList commandList,
        CommandListFileUseCase commandListFileUseCase)
    {
        ArgumentNullException.ThrowIfNull(commandRegistry);
        ArgumentNullException.ThrowIfNull(commandList);
        ArgumentNullException.ThrowIfNull(commandListFileUseCase);
        _commandRegistry = commandRegistry;
        _commandList = commandList;
        _commandListFileUseCase = commandListFileUseCase;

        CommandList.Items.CollectionChanged += CommandItems_CollectionChanged;
        foreach (var item in CommandList.Items)
        {
            AttachCommandItemEvents(item);
        }
    }

    #region OnChanged
    private void OnSelectedLineNumberChanged()
    {
        foreach (var item in CommandList.Items)
        {
            item.IsSelected = false;
        }

        var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
        if (existingItem is not null)
        {
            existingItem.IsSelected = true;
            SelectedItemChanged?.Invoke(existingItem);
            return;
        }

        SelectedItemChanged?.Invoke(null);
        // Refresh()を削除 - IsSelectedはINotifyPropertyChangedで通知される
    }

    private void OnExecutedLineNumberChanged()
    {
        foreach (var item in CommandList.Items)
        {
            item.IsRunning = false;
        }
        
        var cmd = CommandList.Items.FirstOrDefault(x => x.LineNumber == ExecutedLineNumber);
        if (cmd is not null)
        {
            cmd.IsRunning = true;
            // Refresh()を削除 - IsRunningはINotifyPropertyChangedで通知される
        }
    }

    partial void OnSelectedLineNumberChanged(int value)
    {
        OnSelectedLineNumberChanged();
    }

    partial void OnExecutedLineNumberChanged(int value)
    {
        OnExecutedLineNumberChanged();
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
        if (item is null)
        {
            return;
        }

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

        // 追加後にCollectionViewを更新
        CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        UpdateCollapsedState();

        System.Diagnostics.Debug.WriteLine($"コマンドを追加しました: {item.ItemType} -> {_commandRegistry.GetDisplayName(item.ItemType)}");
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
        UpdateCollapsedState();
        // Refresh()を削除
    }

    /// <summary>
    /// 指定位置のアイテムを削除（Undo/Redo用）
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index >= 0 && index < CommandList.Items.Count)
        {
            var previousLineNumber = SelectedLineNumber;
            CommandList.RemoveAt(index);
            UpdateCollapsedState();
            
            if (CommandList.Items.Count == 0)
            {
                SelectedLineNumber = -1;
            }
            else if (index >= CommandList.Items.Count)
            {
                SelectedLineNumber = CommandList.Items.Count - 1;
            }
            else
            {
                SelectedLineNumber = index;
            }

            if (SelectedLineNumber == previousLineNumber)
            {
                // 同じ index が再設定されたケースでも選択状態を同期
                OnSelectedLineNumberChanged();
            }
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
            UpdateCollapsedState();
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
            UpdateCollapsedState();
            // Refresh()を削除
        }
    }

    public void RequestMoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= CommandList.Items.Count ||
            toIndex < 0 || toIndex >= CommandList.Items.Count ||
            fromIndex == toIndex)
        {
            return;
        }

        if (MoveItemRequested is not null)
        {
            MoveItemRequested.Invoke(fromIndex, toIndex);
            return;
        }

        MoveItem(fromIndex, toIndex);
    }

    /// <summary>
    /// アイテムを追加（Undo/Redo用）
    /// </summary>
    public void AddItem(ICommandListItem item)
    {
        CommandList.Add(item);
        UpdateCollapsedState();
        // Refresh()を削除
    }

    public void Up()
    {
        if (SelectedLineNumber == 0) return;

        var selectedBak = SelectedLineNumber;
        CommandList.Move(SelectedLineNumber, SelectedLineNumber - 1);
        SelectedLineNumber = selectedBak - 1;
        UpdateCollapsedState();
    }

    public void Down()
    {
        if (SelectedLineNumber == CommandList.Items.Count - 1) return;

        var selectedBak = SelectedLineNumber;
        CommandList.Move(SelectedLineNumber, SelectedLineNumber + 1);
        SelectedLineNumber = selectedBak + 1;
        UpdateCollapsedState();
    }

    public void Delete()
    {
        var targetItem = SelectedItem;
        if (targetItem is null && SelectedLineNumber >= 0 && SelectedLineNumber < CommandList.Items.Count)
        {
            targetItem = CommandList.Items[SelectedLineNumber];
        }
        if (targetItem is null) return;

        var previousLineNumber = SelectedLineNumber;
        var index = CommandList.Items.IndexOf(targetItem);
        CommandList.Remove(targetItem);
        UpdateCollapsedState();

        if (CommandList.Items.Count == 0)
        {
            SelectedLineNumber = -1;
        }
        else if (index == CommandList.Items.Count)
        {
            SelectedLineNumber = index - 1;
        }
        else
        {
            SelectedLineNumber = index;
        }

        if (SelectedLineNumber == previousLineNumber)
        {
            // 同じ index が再設定されたケースでも選択状態を同期
            OnSelectedLineNumberChanged();
        }
    }

    public void Clear()
    {
        CommandList.Clear();
        _collapsedBlockStarts.Clear();
        UpdateCollapsedState();
        if (SelectedLineNumber != -1)
        {
            SelectedLineNumber = -1;
        }
        else
        {
            SelectedItemChanged?.Invoke(null);
        }
    }

    public void Save(string filePath = "")
    {
        _commandListFileUseCase.Save(CommandList.Items, filePath);
    }

    public void Load(string filePath = "")
    {
        var loadedItems = _commandListFileUseCase.Load(filePath);
        CommandList.Clear();
        foreach (var item in loadedItems)
        {
            CommandList.Add(item);
        }

        if (CommandList.Items.Count > 0)
        {
            SelectedLineNumber = -1;
            SelectedLineNumber = 0;
        }
        else
        {
            if (SelectedLineNumber != -1)
            {
                SelectedLineNumber = -1;
            }
            else
            {
                SelectedItemChanged?.Invoke(null);
            }
        }

        // 読み込み後にレジストリを初期化して表示名が正しく表示されるようにする
        _commandRegistry.Initialize();
        _collapsedBlockStarts.Clear();

        // CollectionViewを更新して日本語表示名を適用
        CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        UpdateCollapsedState();
    }

    public void RequestDelete()
    {
        if (DeleteRequested is not null)
        {
            DeleteRequested.Invoke();
            return;
        }

        Delete();
    }

    /// <summary>
    /// 指定コマンドがブロック開始（IF / LOOP）かを判定する
    /// </summary>
    public bool IsBlockStartCommand(ICommandListItem item)
    {
        if (item is null)
        {
            return false;
        }

        return _commandRegistry.IsIfCommand(item.ItemType) || _commandRegistry.IsLoopCommand(item.ItemType);
    }

    /// <summary>
    /// 指定コマンドの折りたたみ状態を判定する
    /// </summary>
    public bool IsBlockCollapsed(ICommandListItem item)
    {
        return item is not null && _collapsedBlockStarts.Contains(item);
    }

    /// <summary>
    /// 折りたたみ中のブロックに含まれるため非表示とするべき行かどうかを判定する
    /// </summary>
    public bool ShouldHideCommandInCollapsedScope(ICommandListItem item)
    {
        if (item is null)
        {
            return false;
        }

        if (item.IsRunning)
        {
            return false;
        }

        foreach (var start in _collapsedBlockStarts)
        {
            var hasPairedRange = GetCollapsedBlockRange(start, out var startLine, out var endLine);
            if (!hasPairedRange)
            {
                continue;
            }

            if (item.LineNumber > startLine && item.LineNumber <= endLine &&
                (item.NestLevel > start.NestLevel || item.LineNumber == endLine))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 折りたたみ状態を切り替える
    /// </summary>
    public void ToggleBlockCollapse(ICommandListItem item)
    {
        var blockStart = ResolveBlockStartForToggle(item);
        if (blockStart is null)
        {
            return;
        }

        if (_collapsedBlockStarts.Contains(blockStart))
        {
            _collapsedBlockStarts.Remove(blockStart);
        }
        else
        {
            _collapsedBlockStarts.Add(blockStart);
        }

        UpdateCollapsedState();
    }

    private ICommandListItem? ResolveBlockStartForToggle(ICommandListItem item)
    {
        if (item is null)
        {
            return null;
        }

        if (IsBlockStartCommand(item))
        {
            return item;
        }

        return item switch
        {
            ILoopEndItem { Pair: ICommandListItem pair } when IsBlockStartCommand(pair) => pair,
            IIfEndItem { Pair: ICommandListItem pair } when IsBlockStartCommand(pair) => pair,
            IRetryEndItem { Pair: ICommandListItem pair } when IsBlockStartCommand(pair) => pair,
            _ => null
        };
    }

    private void UpdateCollapsedState()
    {
        if (_collapsedBlockStarts.Count == 0)
        {
            if (CollapsedStateVersion != 0)
            {
                CollapsedStateVersion = 0;
            }

            return;
        }

        // 削除済みまたはペア未設定のブロック開始コマンドを除去
        var aliveStarts = _collapsedBlockStarts
            .Where(x => x is not null
                        && IsBlockStartCommand(x)
                        && GetCollapsedBlockRange(x, out _, out _))
            .ToList();
        _collapsedBlockStarts.Clear();
        foreach (var alive in aliveStarts)
        {
            _collapsedBlockStarts.Add(alive);
        }

        CollapsedStateVersion++;
    }

    private static bool GetCollapsedBlockRange(ICommandListItem item, out int startLine, out int endLine)
    {
        switch (item)
        {
            case ILoopItem { Pair.LineNumber: > 0 } loop when loop.Pair!.LineNumber > loop.LineNumber:
                startLine = loop.LineNumber;
                endLine = loop.Pair.LineNumber;
                return true;
            case IIfItem { Pair.LineNumber: > 0 } @if when @if.Pair!.LineNumber > @if.LineNumber:
                startLine = @if.LineNumber;
                endLine = @if.Pair.LineNumber;
                return true;
            default:
                startLine = 0;
                endLine = 0;
                return false;
        }
    }

    private static bool GetEnablePropagationRange(ICommandListItem item, out int startLine, out int endLine)
    {
        switch (item)
        {
            case ILoopItem { Pair.LineNumber: > 0 } loop when loop.Pair!.LineNumber > loop.LineNumber:
                startLine = loop.LineNumber;
                endLine = loop.Pair.LineNumber;
                return true;
            case IIfItem { Pair.LineNumber: > 0 } @if when @if.Pair!.LineNumber > @if.LineNumber:
                startLine = @if.LineNumber;
                endLine = @if.Pair.LineNumber;
                return true;
            case IRetryItem { Pair.LineNumber: > 0 } retry when retry.Pair!.LineNumber > retry.LineNumber:
                startLine = retry.LineNumber;
                endLine = retry.Pair.LineNumber;
                return true;
            default:
                startLine = 0;
                endLine = 0;
                return false;
        }
    }
    #endregion

    #region Call from MainWindowViewModel
    public int GetCount()
    {
        return CommandList.Items.Count;
    }

    public ICommandListItem? GetRunningItem()
    {
        return CommandList.Items.FirstOrDefault(x => x.IsRunning == true);
    }

    public ICommandListItem? GetItem(int lineNumber)
    {
        return CommandList.Items.FirstOrDefault(x => x.LineNumber == lineNumber);
    }

    public void SetRunningState(bool isRunning)
    {
        IsRunning = isRunning;
    }

    public void SetSelectedItem(ICommandListItem? item)
    {
        SelectedItem = item;
    }

    public IReadOnlyList<ICommandListItem> GetSelectedItems()
    {
        return _selectedItems.AsReadOnly();
    }

    public void SetSelectedItems(IReadOnlyList<ICommandListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _selectedItems.Clear();
        _selectedItems.AddRange(items.Where(x => x is not null).Distinct());

        foreach (var commandItem in CommandList.Items)
        {
            commandItem.IsSelected = _selectedItems.Contains(commandItem);
        }

        if (_selectedItems.Count > 0)
        {
            SelectedItemChanged?.Invoke(_selectedItems[0]);
            return;
        }

        SelectedItemChanged?.Invoke(null);
    }

    public void SetSelectedLineNumber(int lineNumber)
    {
        SelectedLineNumber = lineNumber;
    }

    public void SetValidationErrorItems(IEnumerable<ICommandListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        ValidationErrorItems =
        [
            .. items
                .Where(static x => x is not null)
                .Distinct()
        ];
    }

    public void NotifyInteraction()
    {
        InteractionRequested?.Invoke();
    }

    public void Prepare()
    {
        foreach (var item in CommandList.Items)
        {
            item.IsRunning = false;
            item.Progress = 0;
        }
        var view = CollectionViewSource.GetDefaultView(CommandList.Items);
        if (view is IEditableCollectionView editableView)
        {
            if (editableView.IsEditingItem)
            {
                editableView.CommitEdit();
            }
            if (editableView.IsAddingNew)
            {
                editableView.CommitNew();
            }
        }
        view.Refresh();
    }

    /// <summary>
    /// ダブルクリック時の処理を実行
    /// </summary>
    public void OnItemDoubleClick()
    {
        ICommandListItem? selectedItem = null;
        if (SelectedLineNumber >= 0 && SelectedLineNumber < CommandList.Items.Count)
        {
            selectedItem = CommandList.Items[SelectedLineNumber];
        }
        ItemDoubleClicked?.Invoke(selectedItem);
    }
    #endregion

    private void CommandItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<ICommandListItem>())
            {
                DetachCommandItemEvents(oldItem);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<ICommandListItem>())
            {
                AttachCommandItemEvents(newItem);
            }
        }
    }

    private void AttachCommandItemEvents(ICommandListItem item)
    {
        if (item is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += CommandItem_PropertyChanged;
        }
    }

    private void DetachCommandItemEvents(ICommandListItem item)
    {
        if (item is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= CommandItem_PropertyChanged;
        }
    }

    private void CommandItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isPropagatingEnableState || sender is not ICommandListItem item || e.PropertyName != nameof(ICommandListItem.IsEnable))
        {
            return;
        }

        EnableVisualStateVersion++;

        var blockStart = ResolveBlockStartForToggle(item);
        if (blockStart is null || !GetEnablePropagationRange(blockStart, out var startLine, out var endLine))
        {
            return;
        }

        _isPropagatingEnableState = true;
        try
        {
            foreach (var target in CommandList.Items.Where(x => x.LineNumber >= startLine && x.LineNumber <= endLine))
            {
                target.IsEnable = item.IsEnable;
            }
        }
        finally
        {
            _isPropagatingEnableState = false;
        }

        EnableVisualStateVersion++;
    }
}


