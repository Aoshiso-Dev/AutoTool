using AutoTool.Application.History;
namespace AutoTool.Application.History.Commands;

/// <summary>
/// 項目移動の取り消し/やり直しを実装するコマンドです。
/// </summary>
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

    /// <summary>
    /// 項目を移動します。
    /// </summary>
    public void Execute() => _moveAction(_fromIndex, _toIndex);

    /// <summary>
    /// 移動を取り消して元の位置へ戻します。
    /// </summary>
    public void Undo() => _moveAction(_toIndex, _fromIndex);
}
