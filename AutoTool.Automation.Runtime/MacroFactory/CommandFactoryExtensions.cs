using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Automation.Runtime.MacroFactory;

/// <summary>
/// コマンドファクトリ向けの拡張メソッド集
/// </summary>
public static class CommandFactoryExtensions
{
    /// <summary>
    /// 指定した行番号の範囲内にある子要素を取得します
    /// </summary>
    public static IEnumerable<ICommandListItem> GetChildrenBetween(
        this IEnumerable<ICommandListItem> items,
        int startLine,
        int endLine)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Where(x => x.LineNumber > startLine && x.LineNumber < endLine);
    }

    /// <summary>
    /// 指定したネストレベルの要素を取得します
    /// </summary>
    public static IEnumerable<ICommandListItem> GetByNestLevel(
        this IEnumerable<ICommandListItem> items,
        int nestLevel)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Where(x => x.NestLevel == nestLevel);
    }

    /// <summary>
    /// 有効な要素のみを取得します
    /// </summary>
    public static IEnumerable<ICommandListItem> GetEnabled(
        this IEnumerable<ICommandListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Where(x => x.IsEnable);
    }

    /// <summary>
    /// ペア関係を検証します
    /// </summary>
    public static void ValidatePair(this ICommandListItem item, string commandType)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(commandType);

        if (item is IIfItem ifItem && ifItem.Pair is null)
            throw new InvalidOperationException($"{commandType} (行 {item.LineNumber}) に対応する EndIf がありません");

        if (item is ILoopItem loopItem && loopItem.Pair is null)
            throw new InvalidOperationException($"{commandType} (行 {item.LineNumber}) に対応する EndLoop がありません");
    }
}
