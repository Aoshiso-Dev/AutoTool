using AutoTool.Application.Files;
using AutoTool.Application.History;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Application.History.Commands;

namespace AutoTool.Desktop.ViewModel;

public partial class MacroPanelViewModel
{
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
        var selectedItem = _listPanel.SelectedItem;
        var selectedIndex = _listPanel.SelectedLineNumber;

        if (selectedItem is null && selectedIndex >= 0 && selectedIndex < _listPanel.GetCount())
        {
            selectedItem = _listPanel.GetItem(selectedIndex + 1);
        }

        if (selectedItem is not null && _commandHistory is not null)
        {
            var removeCommand = new RemoveItemCommand(
                selectedItem.Clone(),
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

        _editPanel.SetListCount(_listPanel.GetCount());
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
}
