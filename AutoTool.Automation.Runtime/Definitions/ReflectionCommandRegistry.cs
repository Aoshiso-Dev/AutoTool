using System.Linq.Expressions;
using System.Reflection;
using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Automation.Runtime.Definitions;

/// <summary>
/// 実行対象の定義を登録・検索する仕組みを提供します。
/// </summary>
public sealed class ReflectionCommandRegistry(
    ICommandFactory? commandFactory = null,
    IEnumerable<IExternalCommandMetadataProvider>? externalMetadataProviders = null) : ICommandRegistry, ICommandDefinitionProvider
{
    /// <summary>
    /// 一覧の 1 件分として扱うデータを保持し、保存・表示で共通利用できるようにします。
    /// </summary>

    private sealed class Entry
    {
        public required CommandMetadata Metadata { get; init; }
        public required Func<ICommandListItem> ItemFactory { get; init; }
        public required bool HasExecuteAsyncOverride { get; init; }
        public required bool IsCompositeStart { get; init; }
        public required Type CommandType { get; init; }
    }

    private readonly ICommandFactory? _commandFactory = commandFactory;
    private readonly IReadOnlyList<IExternalCommandMetadataProvider> _externalMetadataProviders =
        externalMetadataProviders?.ToList() ?? [];
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private bool _initialized;
    private readonly object _initializeLock = new();

    public void Initialize()
    {
        if (_initialized) return;

        lock (_initializeLock)
        {
            if (_initialized) return;

            _entries.Clear();
            var externalMetadata = _externalMetadataProviders
                .SelectMany(static x => x.GetCommandMetadata())
                .ToArray();
            CommandMetadataCatalog.SetExternalMetadata(externalMetadata);

            foreach (var metadata in CommandMetadataCatalog.GetAll())
            {
                if (_entries.ContainsKey(metadata.TypeName))
                {
                    throw new InvalidOperationException($"重複したコマンド種別が見つかりました: {metadata.TypeName}");
                }

                var hasExecuteAsyncOverride = HasExecuteAsyncOverride(metadata.ItemType);
                _entries[metadata.TypeName] = new Entry
                {
                    Metadata = metadata,
                    ItemFactory = CreateItemFactory(metadata.ItemType),
                    HasExecuteAsyncOverride = hasExecuteAsyncOverride,
                    IsCompositeStart = metadata.IsIfCommand || metadata.IsLoopCommand,
                    CommandType = hasExecuteAsyncOverride ? typeof(SimpleCommand) : metadata.CommandType
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
        if (!TryGetEntry(typeName, out var entry))
        {
            return null;
        }

        var item = entry.ItemFactory();
        item.ItemType = entry.Metadata.TypeName;
        if (item is PluginCommandListItem pluginItem && !string.IsNullOrWhiteSpace(entry.Metadata.PluginId))
        {
            pluginItem.PluginId = entry.Metadata.PluginId;
        }
        return item;
    }

    public bool TryCreateSimple(ICommand parent, ICommandListItem item, out ICommand? command)
    {
        var result = CreateSimple(parent, item);
        command = result.Command;
        return result.Success;
    }

    public CommandCreationResult CreateSimple(ICommand parent, ICommandListItem item)
    {
        if (item?.ItemType is null)
        {
            return CommandCreationResult.Fail(CommandCreationFailureReason.MissingItemType, "ItemType が空です。");
        }

        Initialize();
        if (!TryGetEntry(item.ItemType, out var entry))
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.UnknownItemType,
                $"未知の ItemType です: {item.ItemType}");
        }

        if (entry.IsCompositeStart)
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.MissingCommandBinding,
                $"item type {entry.Metadata.ItemType.Name} は複合コマンドとして処理されます。");
        }

        if (!entry.Metadata.CanCreateCommand)
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.MissingCommandBinding,
                $"コマンド '{entry.Metadata.TypeName}' はまだ実行バインディングが構成されていません。");
        }

        if (_commandFactory is null)
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.CommandFactoryUnavailable,
                "ICommandFactory が利用できません。");
        }

        try
        {
            var settings = item as ICommandSettings ?? new CommandSettings();
            var command = _commandFactory.Create(
                entry.CommandType,
                parent,
                settings,
                item);

            command.LineNumber = item.LineNumber;
            command.IsEnabled = item.IsEnable;
            return CommandCreationResult.Ok(command);
        }
        catch (Exception ex)
        {
            return CommandCreationResult.Fail(
                CommandCreationFailureReason.FactoryException,
                $"コマンド生成に失敗しました: {ex.Message}");
        }
    }

    public bool IsIfCommand(string typeName)
    {
        Initialize();
        return TryGetEntry(typeName, out var entry) && entry.Metadata.IsIfCommand;
    }

    public bool IsLoopCommand(string typeName)
    {
        Initialize();
        return TryGetEntry(typeName, out var entry) && entry.Metadata.IsLoopCommand;
    }

    public bool IsEndCommand(string typeName)
    {
        Initialize();
        return TryGetEntry(typeName, out var entry) && entry.Metadata.IsEndCommand;
    }

    public bool IsStartCommand(string typeName)
    {
        return IsIfCommand(typeName) || IsLoopCommand(typeName);
    }

    public string GetDisplayName(string typeName, string language = "ja")
    {
        Initialize();
        if (!TryGetEntry(typeName, out var entry))
        {
            return typeName;
        }

        return language == "en" ? entry.Metadata.DisplayNameEn : entry.Metadata.DisplayNameJa;
    }

    public string GetCategoryName(string typeName, string language = "ja")
    {
        Initialize();
        if (!TryGetEntry(typeName, out var entry))
        {
            return typeName;
        }

        if (language == "en" && !string.IsNullOrWhiteSpace(entry.Metadata.CustomCategoryNameEn))
        {
            return entry.Metadata.CustomCategoryNameEn;
        }

        if (language != "en" && !string.IsNullOrWhiteSpace(entry.Metadata.CustomCategoryNameJa))
        {
            return entry.Metadata.CustomCategoryNameJa;
        }

        return language == "en"
            ? GetEnglishCategoryName(entry.Metadata.Category)
            : GetJapaneseCategoryName(entry.Metadata.Category);
    }

    public int GetDisplayPriority(string typeName)
    {
        Initialize();
        return TryGetEntry(typeName, out var entry) ? entry.Metadata.DisplayPriority : 9;
    }

    public Type? GetItemType(string typeName)
    {
        Initialize();
        return TryGetEntry(typeName, out var entry) ? entry.Metadata.ItemType : null;
    }

    private bool TryGetEntry(string typeName, out Entry entry)
    {
        entry = null!;
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        if (_entries.TryGetValue(typeName, out var direct) && direct is not null)
        {
            entry = direct;
            return true;
        }

        var normalized = CommandMetadataCatalog.NormalizeTypeName(typeName);
        if (!string.Equals(normalized, typeName, StringComparison.Ordinal)
            && _entries.TryGetValue(normalized, out var mapped)
            && mapped is not null)
        {
            entry = mapped;
            return true;
        }

        return false;
    }

    private static bool HasExecuteAsyncOverride(Type itemType)
    {
        var method = itemType.GetMethod(
            "ExecuteAsync",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            [typeof(ICommandExecutionContext), typeof(CancellationToken)],
            null);

        return method is not null && method.DeclaringType != typeof(CommandListItem);
    }

    private static Func<ICommandListItem> CreateItemFactory(Type itemType)
    {
        var ctor = itemType.GetConstructor(Type.EmptyTypes);
        if (ctor is null)
        {
            throw new InvalidOperationException($"型 {itemType.Name} に引数なしコンストラクタがありません。");
        }

        var body = Expression.Convert(Expression.New(ctor), typeof(ICommandListItem));
        return Expression.Lambda<Func<ICommandListItem>>(body).Compile();
    }

    private static string GetJapaneseCategoryName(CommandCategory category)
    {
        return category switch
        {
            CommandCategory.Action => "クリック操作",
            CommandCategory.Click => "クリック操作",
            CommandCategory.Input => "キー入力",
            CommandCategory.Wait => "待機",
            CommandCategory.Image => "画像操作",
            CommandCategory.Condition => "条件分岐",
            CommandCategory.Control => "繰り返し・リトライ",
            CommandCategory.AI => "その他",
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
            CommandCategory.Click => "Click Operations",
            CommandCategory.Input => "Keyboard Input",
            CommandCategory.Wait => "Wait",
            CommandCategory.Image => "Image Operations",
            CommandCategory.Condition => "Conditions",
            CommandCategory.Control => "Repeat and Retry",
            CommandCategory.AI => "Other",
            CommandCategory.System => "System Operations",
            CommandCategory.Variable => "Variable Operations",
            _ => "Others"
        };
    }
}


