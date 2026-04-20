using AutoTool.Application.History;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.History.Commands;

/// <summary>
/// 一覧全削除の取り消し/やり直しを実装するコマンドです。
/// </summary>
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

    /// <summary>
    /// 一覧を全削除します。
    /// </summary>
    public void Execute() => _clearAction();

    /// <summary>
    /// 退避していた項目一覧を復元します。
    /// </summary>
    public void Undo() => _restoreAction(_savedItems);
}
