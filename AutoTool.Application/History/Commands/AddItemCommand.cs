using AutoTool.Application.History;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.History.Commands;

/// <summary>
/// 項目追加の取り消し/やり直しを実装するコマンドです。
/// </summary>
public class AddItemCommand : IUndoRedoCommand
{
    private readonly Action<ICommandListItem, int> _addAction;
    private readonly Action<int> _removeAction;
    private readonly ICommandListItem _item;
    private readonly int _index;

    public string Description => $"アイテム追加: {_item.ItemType} (行{_index + 1})";

    public AddItemCommand(
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
    /// 指定位置へ項目を追加します。
    /// </summary>
    public void Execute() => _addAction(_item, _index);

    /// <summary>
    /// 追加した項目を取り消します。
    /// </summary>
    public void Undo() => _removeAction(_index);
}
