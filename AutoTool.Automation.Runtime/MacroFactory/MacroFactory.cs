using System.Diagnostics;
using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Automation.Runtime.MacroFactory;

public class MacroFactory(
    ICommandRegistry commandRegistry,
    ICommandFactory commandFactory,
    IEnumerable<ICompositeCommandBuilder> compositeBuilders) : IMacroFactory
{
    private readonly ICommandRegistry _commandRegistry = EnsureNotNull(commandRegistry);
    private readonly ICommandFactory _commandFactory = EnsureNotNull(commandFactory);
    private readonly IReadOnlyList<ICompositeCommandBuilder> _compositeBuilders = EnsureNotNull(compositeBuilders).ToList();
    private readonly object _builderMapLock = new();
    private Dictionary<string, ICompositeCommandBuilder>? _builderByType;

    public ICommand CreateMacro(IEnumerable<ICommandListItem> items)
    {
        _commandRegistry.Initialize();
        EnsureBuilderMapInitialized();

        var sourceItems = MaterializeItems(items);
        var stopwatch = Stopwatch.StartNew();
        MacroBuildMetrics metrics = new();

        var rootCommand = _commandFactory.Create<RootCommand>(null, new CommandSettings());
        var root = _commandFactory.Create<LoopCommand>(rootCommand, new LoopCommandSettings { LoopCount = 1 });

        if (sourceItems.Count == 0)
        {
            root.Children = [];
            LogMetrics(metrics, stopwatch.ElapsedMilliseconds, 0);
            return root;
        }

        root.Children = ListItemToCommand(root, sourceItems, 0, sourceItems.Count - 1, metrics);
        LogMetrics(metrics, stopwatch.ElapsedMilliseconds, sourceItems.Count);
        return root;
    }

    private IEnumerable<ICommand> ListItemToCommand(
        ICommand parent,
        IReadOnlyList<ICommandListItem> items,
        int startIndex,
        int endIndex,
        MacroBuildMetrics metrics)
    {
        if (startIndex > endIndex)
        {
            return [];
        }

        List<ICommand> commands = [];
        var skipUntilLine = int.MinValue;

        for (var index = startIndex; index <= endIndex; index++)
        {
            var item = items[index];
            if (item.LineNumber <= skipUntilLine) continue;
            if (!item.IsEnable) continue;
            Debug.WriteLine($"コマンド生成: 行={item.LineNumber}, 種別={item.ItemType}");

            try
            {
                var command = CreateCommand(parent, item, index, items, startIndex, endIndex, metrics);
                if (command is null)
                {
                    continue;
                }

                commands.Add(command);
                if (TryGetSkipUntilLine(item, out var pairLineNumber) && pairLineNumber > skipUntilLine)
                {
                    skipUntilLine = pairLineNumber;
                }
            }
            catch (Exception ex)
            {
                metrics.RecordFailure(CommandCreationFailureReason.FactoryException);
                Debug.WriteLine($"コマンド生成エラー: 種別={item.ItemType}, 行={item.LineNumber}, 詳細={ex.Message}");
                var commandName = GetCommandDisplayName(item.ItemType);
                throw new InvalidOperationException(
                    $"コマンド '{commandName}' (行 {item.LineNumber}) の生成に失敗しました。原因: {ex.Message}",
                    ex);
            }
        }

        return commands;
    }

    private ICommand? CreateCommand(
        ICommand parent,
        ICommandListItem item,
        int itemIndex,
        IReadOnlyList<ICommandListItem> items,
        int startIndex,
        int endIndex,
        MacroBuildMetrics metrics)
    {
        var simpleResult = _commandRegistry.CreateSimple(parent, item);
        if (simpleResult.Success && simpleResult.Command is not null)
        {
            metrics.RecordSimpleSuccess();
            return simpleResult.Command;
        }

        metrics.RecordSimpleFallback(simpleResult.FailureReason);
        Debug.WriteLine($"シンプルコマンド生成をスキップ: 理由={simpleResult.FailureReason}, 詳細={simpleResult.Message}");

        if (!TryGetCompositeBuilder(item.ItemType, out var builder))
        {
            var commandName = GetCommandDisplayName(item.ItemType);
            throw new UnsupportedCommandTypeException(
                $"未対応のコマンドです: {commandName}",
                item.LineNumber,
                item.ItemType);
        }

        metrics.RecordCompositeBuild();
        return builder.Build(
            parent,
            item,
            itemIndex,
            items,
            startIndex,
            endIndex,
            (p, childStart, childEnd) => ListItemToCommand(p, items, childStart, childEnd, metrics));
    }

    private IReadOnlyList<ICommandListItem> MaterializeItems(IEnumerable<ICommandListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items is IReadOnlyList<ICommandListItem> readOnlyList && IsSortedByLineNumber(readOnlyList))
        {
            return readOnlyList;
        }

        return items
            .OrderBy(static x => x.LineNumber)
            .ToList();
    }

    private static bool IsSortedByLineNumber(IReadOnlyList<ICommandListItem> items)
    {
        for (var i = 1; i < items.Count; i++)
        {
            if (items[i - 1].LineNumber > items[i].LineNumber)
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureBuilderMapInitialized()
    {
        if (_builderByType is not null)
        {
            return;
        }

        lock (_builderMapLock)
        {
            if (_builderByType is not null)
            {
                return;
            }

            var ifBuilder = _compositeBuilders.FirstOrDefault(static x => x.Kind == CompositeCommandKind.If);
            var loopBuilder = _compositeBuilders.FirstOrDefault(static x => x.Kind == CompositeCommandKind.Loop);

            Dictionary<string, ICompositeCommandBuilder> map = new(StringComparer.Ordinal);
            foreach (var typeName in _commandRegistry.GetAllTypeNames())
            {
                if (_commandRegistry.IsIfCommand(typeName) && ifBuilder is not null)
                {
                    map[typeName] = ifBuilder;
                }
                else if (_commandRegistry.IsLoopCommand(typeName) && loopBuilder is not null)
                {
                    map[typeName] = loopBuilder;
                }
            }

            _builderByType = map;
        }
    }

    private bool TryGetCompositeBuilder(string itemType, out ICompositeCommandBuilder builder)
    {
        EnsureBuilderMapInitialized();
        if (_builderByType is not null && _builderByType.TryGetValue(itemType, out builder!))
        {
            return true;
        }

        builder = null!;
        return false;
    }

    private static void LogMetrics(MacroBuildMetrics metrics, long elapsedMilliseconds, int totalItems)
    {
        if (totalItems <= 0)
        {
            return;
        }

        var fallbackSummary = metrics.SimpleFallbacks.Count == 0
            ? "なし"
            : string.Join(", ",
                metrics.SimpleFallbacks
                    .OrderBy(static x => x.Key)
                    .Select(static x => $"{x.Key}={x.Value}"));

        Debug.WriteLine(
            $"マクロ生成メトリクス: Items={totalItems}, ElapsedMs={elapsedMilliseconds}, SimpleSuccess={metrics.SimpleSuccess}, CompositeBuild={metrics.CompositeBuild}, SimpleFallback={metrics.SimpleFallbackTotal}, Failures={metrics.Failures}, Reasons=[{fallbackSummary}]");
    }

    private static bool TryGetSkipUntilLine(ICommandListItem item, out int lineNumber)
    {
        lineNumber = item switch
        {
            IIfItem ifItem when ifItem.Pair is not null => ifItem.Pair.LineNumber,
            ILoopItem loopItem when loopItem.Pair is not null => loopItem.Pair.LineNumber,
            _ => -1
        };

        return lineNumber >= 0;
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

    private sealed class MacroBuildMetrics
    {
        public int SimpleSuccess { get; private set; }
        public int CompositeBuild { get; private set; }
        public int SimpleFallbackTotal { get; private set; }
        public int Failures { get; private set; }
        public Dictionary<CommandCreationFailureReason, int> SimpleFallbacks { get; } = new();

        public void RecordSimpleSuccess() => SimpleSuccess++;

        public void RecordCompositeBuild() => CompositeBuild++;

        public void RecordSimpleFallback(CommandCreationFailureReason reason)
        {
            SimpleFallbackTotal++;
            if (SimpleFallbacks.TryGetValue(reason, out var count))
            {
                SimpleFallbacks[reason] = count + 1;
            }
            else
            {
                SimpleFallbacks[reason] = 1;
            }
        }

        public void RecordFailure(CommandCreationFailureReason reason)
        {
            Failures++;
            if (SimpleFallbacks.TryGetValue(reason, out var count))
            {
                SimpleFallbacks[reason] = count + 1;
            }
            else
            {
                SimpleFallbacks[reason] = 1;
            }
        }
    }
}
