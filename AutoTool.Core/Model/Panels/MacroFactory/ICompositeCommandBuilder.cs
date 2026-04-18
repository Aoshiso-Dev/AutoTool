using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

public enum CompositeCommandKind
{
    If,
    Loop
}

public interface ICompositeCommandBuilder
{
    CompositeCommandKind Kind { get; }

    ICommand Build(
        ICommand parent,
        ICommandListItem item,
        int itemIndex,
        IReadOnlyList<ICommandListItem> items,
        int startIndex,
        int endIndex,
        Func<ICommand, int, int, IEnumerable<ICommand>> buildChildren);
}
