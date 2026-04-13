using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

public sealed class LoopCompositeCommandBuilder : ICompositeCommandBuilder
{
    private readonly ICommandFactory _commandFactory;

    public LoopCompositeCommandBuilder(ICommandFactory commandFactory)
    {
        _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
    }

    public bool CanBuild(ICommandListItem item) => item is ILoopItem;

    public ICommand Build(
        ICommand parent,
        ICommandListItem item,
        IEnumerable<ICommandListItem> items,
        Func<ICommand, IEnumerable<ICommandListItem>, IEnumerable<ICommand>> buildChildren)
    {
        var loopItem = (ILoopItem)item;
        if (loopItem.Pair == null)
        {
            throw new PairMismatchException($"ループ (行 {loopItem.LineNumber}) に対応するEndLoopがありません", loopItem.LineNumber, loopItem.ItemType);
        }

        var endLoopItem = loopItem.Pair;
        var childrenListItems = items
            .Where(x => x.LineNumber > loopItem.LineNumber && x.LineNumber < endLoopItem.LineNumber)
            .ToList();

        if (childrenListItems.Count == 0)
        {
            throw new EmptyStructureException($"ループ (行 {loopItem.LineNumber}) 内にコマンドがありません", loopItem.LineNumber, loopItem.ItemType);
        }

        var endLoopCommand = parent.Children.FirstOrDefault(x => x.LineNumber == endLoopItem.LineNumber) as LoopEndCommand;
        var loopCommand = _commandFactory.Create<LoopCommand>(parent, new LoopCommandSettings
        {
            LoopCount = loopItem.LoopCount,
            Pair = endLoopCommand
        });

        loopCommand.LineNumber = loopItem.LineNumber;
        loopCommand.IsEnabled = loopItem.IsEnable;
        loopCommand.Children = buildChildren(loopCommand, childrenListItems);

        return loopCommand;
    }
}
