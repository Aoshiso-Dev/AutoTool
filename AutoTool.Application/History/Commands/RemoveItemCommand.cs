using AutoTool.Application.History;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.History.Commands;

/// <summary>
/// 項目削除の取り消し/やり直しを実装するコマンドです。
/// </summary>
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

    /// <summary>
    /// 指定位置の項目を削除します。
    /// </summary>
    public void Execute() => _removeAction(_index);

    /// <summary>
    /// 削除した項目を元の位置へ戻します。
    /// </summary>
    public void Undo() => _addAction(_item, _index);
}
