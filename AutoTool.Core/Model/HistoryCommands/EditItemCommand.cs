using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Model;

public class EditItemCommand : IUndoRedoCommand
{
    private readonly Action<ICommandListItem, int> _replaceAction;
    private readonly ICommandListItem _oldItem;
    private readonly ICommandListItem _newItem;
    private readonly int _index;

    public string Description => $"アイテム編集: {_oldItem.ItemType} (行{_index + 1})";

    public EditItemCommand(
        ICommandListItem oldItem,
        ICommandListItem newItem,
        int index,
        Action<ICommandListItem, int> replaceAction)
    {
        _oldItem = oldItem.Clone();
        _newItem = newItem.Clone();
        _index = index;
        _replaceAction = replaceAction;
    }

    public void Execute() => _replaceAction(_newItem, _index);

    public void Undo() => _replaceAction(_oldItem, _index);
}
