using System.Diagnostics;
using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

public class MacroFactory : IMacroFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandRegistry _commandRegistry;
    private readonly ICommandFactory _commandFactory;
    private readonly ICommandEventBus _commandEventBus;
    private readonly IReadOnlyList<ICompositeCommandBuilder> _compositeBuilders;

    public MacroFactory(
        IServiceProvider serviceProvider,
        ICommandRegistry commandRegistry,
        ICommandFactory commandFactory,
        ICommandEventBus commandEventBus,
        IEnumerable<ICompositeCommandBuilder> compositeBuilders)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
        _commandEventBus = commandEventBus ?? throw new ArgumentNullException(nameof(commandEventBus));
        _compositeBuilders = (compositeBuilders ?? throw new ArgumentNullException(nameof(compositeBuilders))).ToList();
    }

    public ICommand CreateMacro(IEnumerable<ICommandListItem> items)
    {
        var cloneItems = items.Select(x => x.Clone()).ToList();
        var rootCommand = _commandFactory.Create<RootCommand>(null, new CommandSettings());
        var root = _commandFactory.Create<LoopCommand>(rootCommand, new LoopCommandSettings { LoopCount = 1 });
        root.Children = ListItemToCommand(root, cloneItems);
        AttachEventBusRecursive(root);
        return root;
    }

    private void AttachEventBusRecursive(ICommand command)
    {
        if (command is BaseCommand baseCommand)
        {
            baseCommand.SetEventBus(_commandEventBus);
        }

        foreach (var child in command.Children)
        {
            AttachEventBusRecursive(child);
        }
    }

    private IEnumerable<ICommand> ListItemToCommand(ICommand parent, IEnumerable<ICommandListItem> items)
    {
        var commands = new List<ICommand>();
        foreach (var item in items)
        {
            if (!item.IsEnable) continue;
            Debug.WriteLine($"Create: {item.LineNumber}, {item.ItemType}");

            try
            {
                var command = CreateCommand(parent, item, items);
                if (command != null) commands.Add(command);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating command for {item.ItemType} at line {item.LineNumber}: {ex.Message}");
                throw new InvalidOperationException($"コマンド '{item.ItemType}' (行 {item.LineNumber}) の生成に失敗しました", ex);
            }
        }

        return commands;
    }

    private ICommand? CreateCommand(ICommand parent, ICommandListItem item, IEnumerable<ICommandListItem> items)
    {
        if (item.IsInLoop || item.IsInIf) return null;

        _commandRegistry.Initialize();

        var simpleResult = _commandRegistry.CreateSimple(parent, item, _serviceProvider);
        if (simpleResult.Success && simpleResult.Command != null)
        {
            return simpleResult.Command;
        }

        Debug.WriteLine($"Simple command creation skipped ({simpleResult.FailureReason}): {simpleResult.Message}");

        var builder = _compositeBuilders.FirstOrDefault(x => x.CanBuild(item));
        if (builder == null)
        {
            throw new UnsupportedCommandTypeException(
                $"未対応のアイテム型です: {item.GetType().Name} (ItemType: {item.ItemType})",
                item.LineNumber,
                item.ItemType);
        }

        return builder.Build(parent, item, items, (p, children) => ListItemToCommand(p, children));
    }
}
