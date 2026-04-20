using AutoTool.Commands.Interface;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Automation.Runtime.MacroFactory;

/// <summary>
/// 複合コマンドの種別です。
/// </summary>
public enum CompositeCommandKind
{
    If,
    Loop
}

/// <summary>
/// 条件やループなど、子コマンドを持つ複合コマンドを構築するための契約です。
/// </summary>
public interface ICompositeCommandBuilder
{
    /// <summary>このビルダーが担当する複合種別です。</summary>
    CompositeCommandKind Kind { get; }

    /// <summary>
    /// 開始アイテムと範囲情報を基に複合コマンドを構築します。
    /// </summary>
    ICommand Build(
        ICommand parent,
        ICommandListItem item,
        int itemIndex,
        IReadOnlyList<ICommandListItem> items,
        int startIndex,
        int endIndex,
        Func<ICommand, int, int, IEnumerable<ICommand>> buildChildren);
}
