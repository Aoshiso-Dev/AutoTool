using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.History.Commands;

public sealed class RemoveItemsCommand : IUndoRedoCommand
{
    private readonly IReadOnlyList<(ICommandListItem Item, int Index)> _items;
    private readonly Action<ICommandListItem, int> _insertAction;
    private readonly Action<int> _removeAtAction;

    public string Description => $"複数アイテム削除 ({_items.Count}件)";

    public RemoveItemsCommand(
        IReadOnlyList<(ICommandListItem Item, int Index)> items,
        Action<ICommandListItem, int> insertAction,
        Action<int> removeAtAction)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(insertAction);
        ArgumentNullException.ThrowIfNull(removeAtAction);

        _items = items;
        _insertAction = insertAction;
        _removeAtAction = removeAtAction;
    }

    public void Execute()
    {
        foreach (var (_, index) in _items.OrderByDescending(x => x.Index))
        {
            _removeAtAction(index);
        }
    }

    public void Undo()
    {
        foreach (var (item, index) in _items.OrderBy(x => x.Index))
        {
            _insertAction(item, index);
        }
    }
}
