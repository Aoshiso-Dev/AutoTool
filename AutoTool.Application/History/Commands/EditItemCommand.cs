using AutoTool.Application.History;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.History.Commands;

/// <summary>
/// 項目編集の取り消し/やり直しを実装するコマンドです。
/// </summary>
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

    /// <summary>
    /// 新しい内容で項目を置換します。
    /// </summary>
    public void Execute() => _replaceAction(_newItem, _index);

    /// <summary>
    /// 編集前の内容へ戻します。
    /// </summary>
    public void Undo() => _replaceAction(_oldItem, _index);
}
