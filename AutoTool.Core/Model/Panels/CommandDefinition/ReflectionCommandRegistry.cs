using System.Reflection;
using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.CommandDefinition;

public sealed class ReflectionCommandRegistry : ICommandRegistry, ICommandDefinitionProvider
{
    private sealed class Entry
    {
        public required CommandMetadata Metadata { get; init; }
        public required Func<ICommandListItem> ItemFactory { get; init; }
    }

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private bool _initialized;
    private readonly object _initializeLock = new();

    public void Initialize()
    {
        if (_initialized) return;

        lock (_initializeLock)
        {
            if (_initialized) return;

            foreach (var metadata in CommandMetadataCatalog.GetAll())
            {
                _entries[metadata.TypeName] = new Entry
                {
                    Metadata = metadata,
                    ItemFactory = CreateItemFactory(metadata.ItemType)
                };
            }

            _initialized = true;
        }
    }

    public IEnumerable<string> GetAllTypeNames()
    {
        Initialize();
        return _entries.Keys.ToArray();
    }

    public IEnumerable<string> GetOrderedTypeNames()
    {
        Initialize();
        return _entries.Values
            .OrderBy(x => x.Metadata.DisplayPriority)
            .ThenBy(x => x.Metadata.DisplaySubPriority)
            .ThenBy(x => x.Metadata.TypeName, StringComparer.Ordinal)
            .Select(x => x.Metadata.TypeName)
            .ToArray();
    }

    public ICommandListItem? CreateCommandItem(string typeName)
    {
        Initialize();
        if (string.IsNullOrWhiteSpace(typeName) || !_entries.TryGetValue(typeName, out var entry))
        {
            return null;
        }

        var item = entry.ItemFactory();
        item.ItemType = typeName;
        return item;
    }

    public bool TryCreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider, out ICommand? command)
    {
        var result = CreateSimple(parent, item, serviceProvider);
        command = result.Command;
        return result.Success;
    }

    public CommandCreationResult CreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider)
    {
        if (item?.ItemType == null)
        {
            return CommandCreationResult.Fail(CommandCreationFailureReason.MissingItemType, "ItemType is empty.");
        }

        Initialize();
        if (!_entries.TryGetValue(item.ItemType, out var entry))
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.UnknownItemType,
                $"Unknown item type: {item.ItemType}");
        }

        var commandFactory = serviceProvider?.GetService(typeof(ICommandFactory)) as ICommandFactory;
        if (commandFactory == null)
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.CommandFactoryUnavailable,
                "ICommandFactory is not available.");
        }

        try
        {
            ICommand command;
            if (HasExecuteAsyncOverride(item.GetType()))
            {
                command = commandFactory.Create(
                    typeof(SimpleCommand),
                    parent,
                    item as ICommandSettings ?? new CommandSettings(),
                    item);
            }
            else
            {
                var bindingAttr = entry.Metadata.ItemType.GetCustomAttribute<SimpleCommandBindingAttribute>();
                if (bindingAttr == null)
                {
                    return CommandCreationResult.Fail(
                        CommandCreationFailureReason.MissingCommandBinding,
                        $"No binding attribute for item type {entry.Metadata.ItemType.Name}.");
                }

                command = commandFactory.Create(
                    bindingAttr.CommandType,
                    parent,
                    item as ICommandSettings ?? new CommandSettings(),
                    item);
            }

            command.LineNumber = item.LineNumber;
            command.IsEnabled = item.IsEnable;
            return CommandCreationResult.Ok(command);
        }
        catch (Exception ex)
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.FactoryException,
                $"Command creation failed: {ex.Message}");
        }
    }

    public bool IsIfCommand(string typeName)
    {
        Initialize();
        return !string.IsNullOrWhiteSpace(typeName)
               && _entries.TryGetValue(typeName, out var entry)
               && entry.Metadata.IsIfCommand;
    }

    public bool IsLoopCommand(string typeName)
    {
        Initialize();
        return !string.IsNullOrWhiteSpace(typeName)
               && _entries.TryGetValue(typeName, out var entry)
               && entry.Metadata.IsLoopCommand;
    }

    public bool IsEndCommand(string typeName)
    {
        Initialize();
        return !string.IsNullOrWhiteSpace(typeName)
               && _entries.TryGetValue(typeName, out var entry)
               && entry.Metadata.IsEndCommand;
    }

    public bool IsStartCommand(string typeName)
    {
        return IsIfCommand(typeName) || IsLoopCommand(typeName);
    }

    public string GetDisplayName(string typeName, string language = "ja")
    {
        Initialize();
        if (!_entries.TryGetValue(typeName, out var entry))
        {
            return typeName;
        }

        return language == "en" ? entry.Metadata.DisplayNameEn : entry.Metadata.DisplayNameJa;
    }

    public string GetCategoryName(string typeName, string language = "ja")
    {
        Initialize();
        if (!_entries.TryGetValue(typeName, out var entry))
        {
            return typeName;
        }

        return language == "en"
            ? GetEnglishCategoryName(entry.Metadata.Category)
            : GetJapaneseCategoryName(entry.Metadata.Category);
    }

    public int GetDisplayPriority(string typeName)
    {
        Initialize();
        return _entries.TryGetValue(typeName, out var entry) ? entry.Metadata.DisplayPriority : 9;
    }

    public Type? GetItemType(string typeName)
    {
        Initialize();
        return _entries.TryGetValue(typeName, out var entry) ? entry.Metadata.ItemType : null;
    }

    private static bool HasExecuteAsyncOverride(Type itemType)
    {
        var method = itemType.GetMethod(
            "ExecuteAsync",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(ICommandExecutionContext), typeof(CancellationToken) },
            null);

        return method != null && method.DeclaringType != typeof(CommandListItem);
    }

    private static Func<ICommandListItem> CreateItemFactory(Type itemType)
    {
        var ctor = itemType.GetConstructor(Type.EmptyTypes);
        if (ctor == null)
        {
            throw new InvalidOperationException($"Type {itemType.Name} does not have a parameterless constructor");
        }

        return () => (ICommandListItem)Activator.CreateInstance(itemType)!;
    }

    private static string GetJapaneseCategoryName(CommandCategory category)
    {
        return category switch
        {
            CommandCategory.Action => "クリック操作",
            CommandCategory.Control => "条件分岐",
            CommandCategory.AI => "AI",
            CommandCategory.System => "システム操作",
            CommandCategory.Variable => "変数操作",
            _ => "その他"
        };
    }

    private static string GetEnglishCategoryName(CommandCategory category)
    {
        return category switch
        {
            CommandCategory.Action => "Click Operations",
            CommandCategory.Control => "Conditional",
            CommandCategory.AI => "AI",
            CommandCategory.System => "System Operations",
            CommandCategory.Variable => "Variable Operations",
            _ => "Others"
        };
    }
}
