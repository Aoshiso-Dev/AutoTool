using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Model;

public class ClearAllCommand : IUndoRedoCommand
{
    private readonly Action _clearAction;
    private readonly Action<IEnumerable<ICommandListItem>> _restoreAction;
    private readonly List<ICommandListItem> _savedItems;

    public string Description => $"全クリア ({_savedItems.Count}アイテム)";

    public ClearAllCommand(
        IEnumerable<ICommandListItem> items,
        Action clearAction,
        Action<IEnumerable<ICommandListItem>> restoreAction)
    {
        _savedItems = [.. items.Select(item => item.Clone())];
        _clearAction = clearAction;
        _restoreAction = restoreAction;
    }

    public void Execute() => _clearAction();

    public void Undo() => _restoreAction(_savedItems);
}
