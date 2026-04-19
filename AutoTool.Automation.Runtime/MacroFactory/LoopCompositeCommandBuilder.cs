using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Automation.Runtime.MacroFactory;

public sealed class LoopCompositeCommandBuilder(ICommandFactory commandFactory) : ICompositeCommandBuilder
{
    private readonly ICommandFactory _commandFactory = EnsureNotNull(commandFactory);

    public CompositeCommandKind Kind => CompositeCommandKind.Loop;

    public ICommand Build(
        ICommand parent,
        ICommandListItem item,
        int itemIndex,
        IReadOnlyList<ICommandListItem> items,
        int startIndex,
        int endIndex,
        Func<ICommand, int, int, IEnumerable<ICommand>> buildChildren)
    {
        var pair = item switch
        {
            ILoopItem loopItem => loopItem.Pair,
            IRetryItem retryItem => retryItem.Pair,
            _ => null
        };

        if (pair is null)
        {
            throw new PairMismatchException($"ループ系コマンド (行 {item.LineNumber}) に対応する終了コマンドがありません", item.LineNumber, item.ItemType);
        }

        var endLoopIndex = FindIndexByLineNumber(items, pair.LineNumber, itemIndex + 1, endIndex);
        if (endLoopIndex < 0)
        {
            throw new PairMismatchException($"ループ系コマンド (行 {item.LineNumber}) の終了位置を特定できません", item.LineNumber, item.ItemType);
        }

        if (endLoopIndex - itemIndex <= 1)
        {
            throw new EmptyStructureException($"ループ系コマンド (行 {item.LineNumber}) 内にコマンドがありません", item.LineNumber, item.ItemType);
        }

        ICommand loopCommand = item switch
        {
            ILoopItem loopItem => _commandFactory.Create<LoopCommand>(parent, new LoopCommandSettings
            {
                LoopCount = loopItem.LoopCount,
                Pair = null
            }),
            IRetryItem retryItem => _commandFactory.Create<RetryCommand>(parent, new RetryCommandSettings
            {
                RetryCount = retryItem.RetryCount,
                RetryInterval = retryItem.RetryInterval
            }),
            _ => throw new UnsupportedCommandTypeException($"未対応のループ系コマンドです: {item.GetType().Name}", item.LineNumber, item.ItemType)
        };

        loopCommand.LineNumber = item.LineNumber;
        loopCommand.IsEnabled = item.IsEnable;
        loopCommand.Children = buildChildren(loopCommand, itemIndex + 1, endLoopIndex - 1);

        return loopCommand;
    }

    private static int FindIndexByLineNumber(IReadOnlyList<ICommandListItem> items, int targetLineNumber, int fromIndex, int toIndex)
    {
        for (var i = fromIndex; i <= toIndex; i++)
        {
            var lineNumber = items[i].LineNumber;
            if (lineNumber == targetLineNumber)
            {
                return i;
            }

            if (lineNumber > targetLineNumber)
            {
                break;
            }
        }

        return -1;
    }

    private static ICommandFactory EnsureNotNull(ICommandFactory value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}
