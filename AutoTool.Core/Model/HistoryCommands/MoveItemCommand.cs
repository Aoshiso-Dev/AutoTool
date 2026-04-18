namespace AutoTool.Model;

public class MoveItemCommand : IUndoRedoCommand
{
    private readonly Action<int, int> _moveAction;
    private readonly int _fromIndex;
    private readonly int _toIndex;

    public string Description => $"アイテム移動: 行{_fromIndex + 1} → 行{_toIndex + 1}";

    public MoveItemCommand(int fromIndex, int toIndex, Action<int, int> moveAction)
    {
        _fromIndex = fromIndex;
        _toIndex = toIndex;
        _moveAction = moveAction;
    }

    public void Execute() => _moveAction(_fromIndex, _toIndex);

    public void Undo() => _moveAction(_toIndex, _fromIndex);
}
