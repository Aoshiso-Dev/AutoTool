using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Model;

public class RemoveItemCommand : IUndoRedoCommand
{
    private readonly Action<ICommandListItem, int> _addAction;
    private readonly Action<int> _removeAction;
    private readonly ICommandListItem _item;
    private readonly int _index;

    public string Description => $"アイテム削除: {_item.ItemType} (行{_index + 1})";

    public RemoveItemCommand(
        ICommandListItem item,
        int index,
        Action<ICommandListItem, int> addAction,
        Action<int> removeAction)
    {
        _item = item;
        _index = index;
        _addAction = addAction;
        _removeAction = removeAction;
    }

    public void Execute() => _removeAction(_index);

    public void Undo() => _addAction(_item, _index);
}
