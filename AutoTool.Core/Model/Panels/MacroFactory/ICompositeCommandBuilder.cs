using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

public interface ICompositeCommandBuilder
{
    bool CanBuild(ICommandListItem item);

    ICommand Build(
        ICommand parent,
        ICommandListItem item,
        IEnumerable<ICommandListItem> items,
        Func<ICommand, IEnumerable<ICommandListItem>, IEnumerable<ICommand>> buildChildren);
}
