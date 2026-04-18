using System.Diagnostics;
using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

public class MacroFactory(
    IServiceProvider serviceProvider,
    ICommandRegistry commandRegistry,
    ICommandFactory commandFactory,
    ICommandEventBus commandEventBus,
    IEnumerable<ICompositeCommandBuilder> compositeBuilders) : IMacroFactory
{
    private readonly IServiceProvider _serviceProvider = EnsureNotNull(serviceProvider);
    private readonly ICommandRegistry _commandRegistry = EnsureNotNull(commandRegistry);
    private readonly ICommandFactory _commandFactory = EnsureNotNull(commandFactory);
    private readonly ICommandEventBus _commandEventBus = EnsureNotNull(commandEventBus);
    private readonly IReadOnlyList<ICompositeCommandBuilder> _compositeBuilders = EnsureNotNull(compositeBuilders).ToList();

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
        List<ICommand> commands = [];
        HashSet<int> skippedLines = [];
        foreach (var item in items)
        {
            if (skippedLines.Contains(item.LineNumber)) continue;
            if (!item.IsEnable) continue;
            Debug.WriteLine($"コマンド生成: 行={item.LineNumber}, 種別={item.ItemType}");

            try
            {
                var command = CreateCommand(parent, item, items);
                if (command is not null)
                {
                    commands.Add(command);
                    MarkChildLinesAsSkipped(item, skippedLines);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"コマンド生成エラー: 種別={item.ItemType}, 行={item.LineNumber}, 詳細={ex.Message}");
                var commandName = GetCommandDisplayName(item.ItemType);
                throw new InvalidOperationException(
                    $"コマンド '{commandName}' (行 {item.LineNumber}) の生成に失敗しました。原因: {ex.Message}",
                    ex);
            }
        }

        return commands;
    }

    private ICommand? CreateCommand(ICommand parent, ICommandListItem item, IEnumerable<ICommandListItem> items)
    {
        _commandRegistry.Initialize();

        var simpleResult = _commandRegistry.CreateSimple(parent, item, _serviceProvider);
        if (simpleResult.Success && simpleResult.Command is not null)
        {
            return simpleResult.Command;
        }

        Debug.WriteLine($"シンプルコマンド生成をスキップ: 理由={simpleResult.FailureReason}, 詳細={simpleResult.Message}");

        var builder = _compositeBuilders.FirstOrDefault(x => x.CanBuild(item));
        if (builder is null)
        {
            var commandName = GetCommandDisplayName(item.ItemType);
            throw new UnsupportedCommandTypeException(
                $"未対応のコマンドです: {commandName}",
                item.LineNumber,
                item.ItemType);
        }

        return builder.Build(parent, item, items, (p, children) => ListItemToCommand(p, children));
    }

    private static void MarkChildLinesAsSkipped(ICommandListItem item, ISet<int> skippedLines)
    {
        Action mark = item switch
        {
            IIfItem ifItem when ifItem.Pair is not null
                => () => MarkRange(ifItem.LineNumber + 1, ifItem.Pair.LineNumber, skippedLines),
            ILoopItem loopItem when loopItem.Pair is not null
                => () => MarkRange(loopItem.LineNumber + 1, loopItem.Pair.LineNumber, skippedLines),
            _ => static () => { }
        };

        mark();
    }

    private static void MarkRange(int fromInclusive, int toInclusive, ISet<int> set)
    {
        for (var line = fromInclusive; line <= toInclusive; line++)
        {
            set.Add(line);
        }
    }

    private static string GetCommandDisplayName(string itemType)
    {
        var displayName = CommandListItem.GetDisplayNameForType(itemType);
        return string.Equals(displayName, itemType, StringComparison.Ordinal)
            ? itemType
            : displayName;
    }

    private static T EnsureNotNull<T>(T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}
