using AutoTool.Application.Files;
using AutoTool.Application.History;
using AutoTool.Application.History.Commands;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Desktop.ViewModel;

public partial class MacroPanelViewModel
{
    private IReadOnlyList<ICommandListItem> _inAppCommandClipboardItems = [];

    private void HandleClear()
    {
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

        _editPanel.SetListCount(_listPanel.GetCount());
    }

    private void HandleAdd(string itemType)
    {
        if (_commandHistory is not null)
        {
            var newItem = _commandRegistry.CreateCommandItem(itemType);
            if (newItem is not null)
            {
                var targetIndex = _listPanel.SelectedLineNumber + 1;
                var addCommand = new AddItemCommand(
                    newItem,
                    targetIndex,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index)
                );
                _commandHistory.ExecuteCommand(addCommand);
            }
        }
        else
        {
            _listPanel.Add(itemType);
        }

        _editPanel.SetListCount(_listPanel.GetCount());
    }

    private void HandleUp()
    {
        var fromIndex = _listPanel.SelectedLineNumber;
        var toIndex = fromIndex - 1;

        if (toIndex >= 0 && _commandHistory is not null)
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
    }

    private void HandleDown()
    {
        var fromIndex = _listPanel.SelectedLineNumber;
        var toIndex = fromIndex + 1;

        if (toIndex < _listPanel.GetCount() && _commandHistory is not null)
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
    }

    private void HandleDelete()
    {
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
                return;
            }

            selectedItems.Add(selectedItem);
        }

        if (selectedItems.Count == 1)
        {
            var target = selectedItems[0];
            var selectedIndex = _listPanel.CommandList.Items.IndexOf(target);
            if (selectedIndex < 0)
            {
                return;
            }

            if (_commandHistory is not null)
            {
                var removeCommand = new RemoveItemCommand(
                    target.Clone(),
                    selectedIndex,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index)
                );
                _commandHistory.ExecuteCommand(removeCommand);
            }
            else
            {
                _listPanel.Delete();
            }
        }
        else
        {
            var entries = selectedItems
                .Select(item => (Item: item.Clone(), Index: _listPanel.CommandList.Items.IndexOf(item)))
                .Where(x => x.Index >= 0)
                .OrderBy(x => x.Index)
                .ToList();

            if (entries.Count == 0)
            {
                return;
            }

            if (_commandHistory is not null)
            {
                var removeCommand = new RemoveItemsCommand(
                    entries,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index));
                _commandHistory.ExecuteCommand(removeCommand);
            }
            else
            {
                foreach (var (_, index) in entries.OrderByDescending(x => x.Index))
                {
                    _listPanel.RemoveAt(index);
                }
            }
        }

        _editPanel.SetListCount(_listPanel.GetCount());
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
    }

    private void HandleItemDoubleClick(ICommandListItem? item)
    {
        if (item is null || IsRunning) return;

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
