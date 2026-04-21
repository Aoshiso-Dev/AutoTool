using AutoTool.Application.Files;
using AutoTool.Application.History;
using AutoTool.Application.History.Commands;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Runtime.Lists;

namespace AutoTool.Desktop.ViewModel;

/// <summary>
/// マクロ編集パネルの状態と操作を管理する ViewModel です。
/// </summary>
public partial class MacroPanelViewModel
{
    private IReadOnlyList<ICommandListItem> _inAppCommandClipboardItems = [];

    private void HandleClear()
    {
        ClosePreflightPanelForListInteraction();
        var countBefore = _listPanel.GetCount();
        if (_commandHistory is not null)
        {
            var clearCommand = new ClearAllCommand(
                _listPanel.CommandList.Items.ToList(),
                () => _listPanel.Clear(),
                RestoreItems
            );
            _commandHistory.ExecuteCommand(clearCommand);
        }
        else
        {
            _listPanel.Clear();
        }

        var countAfter = _listPanel.GetCount();
        _editPanel.SetListCount(countAfter);
        if (countBefore > 0 && countAfter == 0)
        {
            RequestNewFileState();
        }

        PublishStatusMessage(countBefore > 0 ? $"コマンドを全削除しました（{countBefore}件）。" : "削除対象のコマンドはありません。");
    }

    private void HandleAdd(string itemType)
    {
        ClosePreflightPanelForListInteraction();
        var itemsToAdd = CreateItemsForAdd(itemType);
        if (itemsToAdd.Count == 0)
        {
            PublishStatusMessage("コマンド追加に失敗しました。");
            return;
        }

        var targetIndex = _listPanel.SelectedLineNumber >= 0
            ? _listPanel.SelectedLineNumber + 1
            : _listPanel.GetCount();

        if (_commandHistory is not null)
        {
            if (itemsToAdd.Count == 1)
            {
                var addCommand = new AddItemCommand(
                    itemsToAdd[0],
                    targetIndex,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index));
                _commandHistory.ExecuteCommand(addCommand);
            }
            else
            {
                var addCommand = new AddItemsCommand(
                    itemsToAdd,
                    targetIndex,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index));
                _commandHistory.ExecuteCommand(addCommand);
            }
        }
        else
        {
            for (var i = 0; i < itemsToAdd.Count; i++)
            {
                _listPanel.InsertAt(targetIndex + i, itemsToAdd[i]);
            }
        }

        _editPanel.SetListCount(_listPanel.GetCount());
        var addedCommandName = CommandListItem.GetDisplayNameForType(itemType);
        PublishStatusMessage(itemsToAdd.Count > 1
            ? $"{addedCommandName}（開始/終了）を追加しました。"
            : $"{addedCommandName} を追加しました。");
    }

    private void HandleUp()
    {
        ClosePreflightPanelForListInteraction();
        var fromIndex = _listPanel.SelectedLineNumber;
        var toIndex = fromIndex - 1;
        if (fromIndex <= 0)
        {
            PublishStatusMessage("これ以上上へ移動できません。");
            return;
        }

        if (_commandHistory is not null)
        {
            var moveCommand = new MoveItemCommand(
                fromIndex,
                toIndex,
                (from, to) => _listPanel.MoveItem(from, to)
            );
            _commandHistory.ExecuteCommand(moveCommand);
        }
        else
        {
            _listPanel.Up();
        }

        PublishStatusMessage("コマンドを上へ移動しました。");
    }

    private void HandleDown()
    {
        ClosePreflightPanelForListInteraction();
        var fromIndex = _listPanel.SelectedLineNumber;
        var toIndex = fromIndex + 1;
        if (fromIndex < 0 || toIndex >= _listPanel.GetCount())
        {
            PublishStatusMessage("これ以上下へ移動できません。");
            return;
        }

        if (_commandHistory is not null)
        {
            var moveCommand = new MoveItemCommand(
                fromIndex,
                toIndex,
                (from, to) => _listPanel.MoveItem(from, to)
            );
            _commandHistory.ExecuteCommand(moveCommand);
        }
        else
        {
            _listPanel.Down();
        }

        PublishStatusMessage("コマンドを下へ移動しました。");
    }

    private void HandleMoveItemRequested(int fromIndex, int toIndex)
    {
        ClosePreflightPanelForListInteraction();
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
        {
            return;
        }

        if (_commandHistory is not null)
        {
            var moveCommand = new MoveItemCommand(
                fromIndex,
                toIndex,
                (from, to) => _listPanel.MoveItem(from, to)
            );
            _commandHistory.ExecuteCommand(moveCommand);
        }
        else
        {
            _listPanel.MoveItem(fromIndex, toIndex);
        }
    }

    private void HandleDelete()
    {
        ClosePreflightPanelForListInteraction();
        var countBefore = _listPanel.GetCount();
        var selectedItems = _listPanel.GetSelectedItems()
            .Distinct()
            .ToList();

        if (selectedItems.Count == 0)
        {
            var selectedItem = _listPanel.SelectedItem;
            var selectedIndex = _listPanel.SelectedLineNumber;

            if (selectedItem is null && selectedIndex >= 0 && selectedIndex < _listPanel.GetCount())
            {
                selectedItem = _listPanel.GetItem(selectedIndex + 1);
            }

            if (selectedItem is null)
            {
                PublishStatusMessage("削除するコマンドを選択してください。");
                return;
            }

            selectedItems.Add(selectedItem);
        }

        var entries = ExpandDeleteEntriesWithPairs(selectedItems);
        if (entries.Count == 0)
        {
            PublishStatusMessage("削除対象のコマンドが見つかりませんでした。");
            return;
        }

        if (_commandHistory is not null)
        {
            if (entries.Count == 1)
            {
                var entry = entries[0];
                var removeCommand = new RemoveItemCommand(
                    entry.Item.Clone(),
                    entry.Index,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index));
                _commandHistory.ExecuteCommand(removeCommand);
            }
            else
            {
                var removeCommand = new RemoveItemsCommand(
                    entries,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index));
                _commandHistory.ExecuteCommand(removeCommand);
            }
        }
        else
        {
            foreach (var (_, index) in entries.OrderByDescending(x => x.Index))
            {
                _listPanel.RemoveAt(index);
            }
        }

        var countAfter = _listPanel.GetCount();
        _editPanel.SetListCount(countAfter);
        if (countBefore > 0 && countAfter == 0)
        {
            RequestNewFileState();
        }

        PublishStatusMessage($"{entries.Count}件のコマンドを削除しました。");
    }

    private void HandleCopy()
    {
        var selectedItems = _listPanel.GetSelectedItems()
            .Distinct()
            .ToList();

        if (selectedItems.Count == 0)
        {
            var selectedItem = _listPanel.SelectedItem;
            if (selectedItem is null && _listPanel.SelectedLineNumber >= 0 && _listPanel.SelectedLineNumber < _listPanel.GetCount())
            {
                selectedItem = _listPanel.GetItem(_listPanel.SelectedLineNumber + 1);
            }

            if (selectedItem is not null)
            {
                selectedItems.Add(selectedItem);
            }
        }

        if (selectedItems.Count == 0)
        {
            PublishStatusMessage("コピーするコマンドを選択してください。");
            return;
        }

        try
        {
            _inAppCommandClipboardItems = selectedItems
                .Select(x => x.Clone())
                .Select(item =>
                {
                    PrepareItemForPaste(item);
                    return item;
                })
                .ToList();

            PublishStatusMessage($"{_inAppCommandClipboardItems.Count}件のコマンドをコピーしました。");
        }
        catch (Exception ex)
        {
            PublishStatusMessage("コピーに失敗しました。");
            _notifier.ShowError($"コピーに失敗しました。\n{ex.Message}", "コピー");
        }
    }

    private void HandlePaste()
    {
        ClosePreflightPanelForListInteraction();
        if (!TryReadInAppClipboardItems(out var copiedItems))
        {
            PublishStatusMessage("コピー済みのコマンドがありません。");
            return;
        }

        var selectedItems = _listPanel.GetSelectedItems()
            .Distinct()
            .ToList();
        var targetIndex = selectedItems.Count > 0
            ? selectedItems
                .Select(x => _listPanel.CommandList.Items.IndexOf(x))
                .Where(x => x >= 0)
                .DefaultIfEmpty(_listPanel.SelectedLineNumber)
                .Max() + 1
            : _listPanel.SelectedLineNumber >= 0 ? _listPanel.SelectedLineNumber + 1 : _listPanel.GetCount();

        if (targetIndex < 0)
        {
            targetIndex = _listPanel.GetCount();
        }

        if (_commandHistory is not null)
        {
            var addCommand = new AddItemsCommand(
                copiedItems,
                targetIndex,
                (item, index) => _listPanel.InsertAt(index, item),
                index => _listPanel.RemoveAt(index));
            _commandHistory.ExecuteCommand(addCommand);
        }
        else
        {
            for (var i = 0; i < copiedItems.Count; i++)
            {
                _listPanel.InsertAt(targetIndex + i, copiedItems[i]);
            }
        }

        _editPanel.SetListCount(_listPanel.GetCount());
        PublishStatusMessage($"{copiedItems.Count}件のコマンドを貼り付けました。");
    }

    private void HandleEdit(ICommandListItem? item)
    {
        if (item is null) return;
        if (_isEditDialogOpen) return;
        ClosePreflightPanelForListInteraction();

        var oldItem = _listPanel.SelectedItem;
        var index = item.LineNumber - 1;

        if (oldItem is not null && _commandHistory is not null)
        {
            var editCommand = new EditItemCommand(
                oldItem,
                item,
                index,
                (editedItem, editIndex) => _listPanel.ReplaceAt(editIndex, editedItem)
            );
            _commandHistory.ExecuteCommand(editCommand);
        }
        else
        {
            _listPanel.ReplaceAt(index, item);
        }

        var current = _listPanel.GetItem(index + 1);
        UpdateValidationErrorForEditedItem(oldItem, current);
    }

    private void HandleItemDoubleClick(ICommandListItem? item)
    {
        if (item is null || IsRunning) return;
        ClosePreflightPanelForListInteraction();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var backup = item.Clone();
            var editingItem = item.Clone();
            var index = item.LineNumber - 1;

            _isEditDialogOpen = true;
            try
            {
                _editPanel.SetItem(editingItem);

                var editWindow = new AutoTool.Desktop.Panels.View.EditPanelWindow();
                editWindow.SetEditPanelDataContext(EditPanelViewModel);
                editWindow.Owner = System.Windows.Application.Current.MainWindow;

                var result = editWindow.ShowDialog();
                if (result == true)
                {
                    if (_commandHistory is not null)
                    {
                        var editCommand = new EditItemCommand(
                            backup,
                            editingItem,
                            index,
                            (editedItem, editIndex) => _listPanel.ReplaceAt(editIndex, editedItem)
                        );
                        _commandHistory.ExecuteCommand(editCommand);
                    }
                    else
                    {
                        _listPanel.ReplaceAt(index, editingItem);
                    }
                }

                var current = _listPanel.GetItem(index + 1);
                UpdateValidationErrorForEditedItem(backup, current);
                if (current is not null)
                {
                    _editPanel.SetItem(current);
                }
            }
            finally
            {
                _isEditDialogOpen = false;
            }
        });
    }

    private void RestoreItems(IEnumerable<ICommandListItem> items)
    {
        _listPanel.Clear();
        foreach (var item in items)
        {
            _listPanel.AddItem(item.Clone());
        }

        _editPanel.SetListCount(_listPanel.GetCount());
    }

    private static void PrepareItemForPaste(ICommandListItem item)
    {
        item.LineNumber = 0;
        item.IsRunning = false;
        item.IsSelected = false;
        item.Progress = 0;
        item.NestLevel = 0;
        item.IsInLoop = false;
        item.IsInIf = false;

        if (item is IIfItem ifItem)
        {
            ifItem.Pair = null;
        }

        if (item is IIfEndItem ifEndItem)
        {
            ifEndItem.Pair = null;
        }

        if (item is ILoopItem loopItem)
        {
            loopItem.Pair = null;
        }

        if (item is ILoopEndItem loopEndItem)
        {
            loopEndItem.Pair = null;
        }

        if (item is IRetryItem retryItem)
        {
            retryItem.Pair = null;
        }

        if (item is IRetryEndItem retryEndItem)
        {
            retryEndItem.Pair = null;
        }
    }

    private List<ICommandListItem> CreateItemsForAdd(string itemType)
    {
        var startItem = _commandRegistry.CreateCommandItem(itemType);
        if (startItem is null)
        {
            return [];
        }

        startItem.ItemType = itemType;
        var result = new List<ICommandListItem> { startItem };

        var endType = GetAutoPairedEndType(itemType);
        if (string.IsNullOrWhiteSpace(endType))
        {
            return result;
        }

        var endItem = _commandRegistry.CreateCommandItem(endType);
        if (endItem is null)
        {
            return result;
        }

        endItem.ItemType = endType;
        result.Add(endItem);
        return result;
    }

    private static string? GetAutoPairedEndType(string itemType)
    {
        return itemType switch
        {
            CommandTypeNames.Loop => CommandTypeNames.LoopEnd,
            CommandTypeNames.Retry => CommandTypeNames.RetryEnd,
            CommandTypeNames.IfImageExist
                or CommandTypeNames.IfImageNotExist
                or CommandTypeNames.IfTextExist
                or CommandTypeNames.IfTextNotExist
                or CommandTypeNames.IfImageExistAI
                or CommandTypeNames.IfImageNotExistAI
                or CommandTypeNames.IfVariable => CommandTypeNames.IfEnd,
            _ => null
        };
    }

    private static ICommandListItem? GetPairedItem(ICommandListItem item)
    {
        return item switch
        {
            IIfItem ifItem => ifItem.Pair,
            IIfEndItem ifEndItem => ifEndItem.Pair,
            ILoopItem loopItem => loopItem.Pair,
            ILoopEndItem loopEndItem => loopEndItem.Pair,
            IRetryItem retryItem => retryItem.Pair,
            IRetryEndItem retryEndItem => retryEndItem.Pair,
            _ => null
        };
    }

    private List<(ICommandListItem Item, int Index)> ExpandDeleteEntriesWithPairs(IReadOnlyCollection<ICommandListItem> selectedItems)
    {
        var indexSet = new HashSet<int>();
        foreach (var item in selectedItems)
        {
            var index = _listPanel.CommandList.Items.IndexOf(item);
            if (index >= 0)
            {
                indexSet.Add(index);
            }

            var pair = GetPairedItem(item);
            if (pair is null)
            {
                continue;
            }

            var pairIndex = _listPanel.CommandList.Items.IndexOf(pair);
            if (pairIndex >= 0)
            {
                indexSet.Add(pairIndex);
            }
        }

        return indexSet
            .OrderBy(x => x)
            .Select(index => (Item: _listPanel.CommandList.Items[index].Clone(), Index: index))
            .ToList();
    }

    private bool TryReadInAppClipboardItems(out IReadOnlyList<ICommandListItem> items)
    {
        items = [];
        if (_inAppCommandClipboardItems.Count == 0)
        {
            return false;
        }

        items = _inAppCommandClipboardItems
            .Select(x => x.Clone())
            .Select(item =>
            {
                PrepareItemForPaste(item);
                return item;
            })
            .ToList();

        return true;
    }
}
