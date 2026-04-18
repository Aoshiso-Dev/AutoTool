using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

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
        var loopItem = (ILoopItem)item;
        if (loopItem.Pair is null)
        {
            throw new PairMismatchException($"ループ (行 {loopItem.LineNumber}) に対応するEndLoopがありません", loopItem.LineNumber, loopItem.ItemType);
        }

        var endLoopIndex = FindIndexByLineNumber(items, loopItem.Pair.LineNumber, itemIndex + 1, endIndex);
        if (endLoopIndex < 0)
        {
            throw new PairMismatchException($"ループ (行 {loopItem.LineNumber}) のEndLoop位置を特定できません", loopItem.LineNumber, loopItem.ItemType);
        }

        if (endLoopIndex - itemIndex <= 1)
        {
            throw new EmptyStructureException($"ループ (行 {loopItem.LineNumber}) 内にコマンドがありません", loopItem.LineNumber, loopItem.ItemType);
        }

        var loopCommand = _commandFactory.Create<LoopCommand>(parent, new LoopCommandSettings
        {
            LoopCount = loopItem.LoopCount,
            Pair = null
        });

        loopCommand.LineNumber = loopItem.LineNumber;
        loopCommand.IsEnabled = loopItem.IsEnable;
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
