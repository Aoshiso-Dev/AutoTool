using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.History.Commands;

public sealed class AddItemsCommand : IUndoRedoCommand
{
    private readonly IReadOnlyList<ICommandListItem> _items;
    private readonly int _index;
    private readonly Action<ICommandListItem, int> _insertAction;
    private readonly Action<int> _removeAtAction;

    public string Description => $"複数アイテム追加 ({_items.Count}件)";

    public AddItemsCommand(
        IReadOnlyList<ICommandListItem> items,
        int index,
        Action<ICommandListItem, int> insertAction,
        Action<int> removeAtAction)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(insertAction);
        ArgumentNullException.ThrowIfNull(removeAtAction);

        _items = items;
        _index = index;
        _insertAction = insertAction;
        _removeAtAction = removeAtAction;
    }

    public void Execute()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _insertAction(_items[i], _index + i);
        }
    }

    public void Undo()
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            _removeAtAction(_index + i);
        }
    }
}
