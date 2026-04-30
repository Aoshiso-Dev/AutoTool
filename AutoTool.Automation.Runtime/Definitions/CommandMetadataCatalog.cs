using System.Collections.ObjectModel;
using System.Reflection;

namespace AutoTool.Automation.Runtime.Definitions;

/// <summary>
/// コマンド定義の表示名・型名・カテゴリなどのメタデータを保持し、一覧表示や生成処理から参照できるようにします。
/// </summary>
public sealed class CommandMetadata
{
    public required string TypeName { get; init; }
    public required Type ItemType { get; init; }
    public required Type CommandType { get; init; }
    public required Type SettingsType { get; init; }
    public required CommandCategory Category { get; init; }
    public required bool IsIfCommand { get; init; }
    public required bool IsLoopCommand { get; init; }
    public required bool IsEndCommand { get; init; }
    public required int DisplayPriority { get; init; }
    public required int DisplaySubPriority { get; init; }
    public required string DisplayNameJa { get; init; }
    public required string DisplayNameEn { get; init; }
    public string? CustomCategoryNameJa { get; init; }
    public string? CustomCategoryNameEn { get; init; }
    public bool CanCreateCommand { get; init; } = true;
    public bool ShowInCommandList { get; init; } = true;
    public string? PluginId { get; init; }
}

/// <summary>
/// 定義情報を収集して一覧として提供します。
/// </summary>
public static class CommandMetadataCatalog
{
    private static readonly object ExternalMetadataLock = new();
    private static readonly IReadOnlyDictionary<string, string> LegacyTypeAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EndLoop"] = CommandTypeNames.LoopEnd,
            ["LoopEnd"] = CommandTypeNames.LoopEnd,
            ["EndIf"] = CommandTypeNames.IfEnd,
            ["IfEnd"] = CommandTypeNames.IfEnd,
            ["RetryEnd"] = CommandTypeNames.RetryEnd
        };

    private static readonly Lazy<IReadOnlyDictionary<string, CommandMetadata>> ByTypeName =
        new(CreateByTypeName);
    private static IReadOnlyDictionary<string, CommandMetadata> _externalByTypeName =
        new ReadOnlyDictionary<string, CommandMetadata>(new Dictionary<string, CommandMetadata>(StringComparer.Ordinal));

    private static readonly Lazy<IReadOnlyDictionary<Type, CommandMetadata>> ByItemType =
        new(() => ByTypeName.Value.Values.ToDictionary(x => x.ItemType, x => x));

    public static IReadOnlyCollection<CommandMetadata> GetAll()
    {
        lock (ExternalMetadataLock)
        {
            return ByTypeName.Value.Values
                .Concat(_externalByTypeName.Values)
                .ToArray();
        }
    }

    public static bool TryGetByTypeName(string typeName, out CommandMetadata metadata)
    {
        var normalized = NormalizeTypeName(typeName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            metadata = null!;
            return false;
        }

        lock (ExternalMetadataLock)
        {
            if (_externalByTypeName.TryGetValue(normalized, out metadata!))
            {
                return true;
            }

            return ByTypeName.Value.TryGetValue(normalized, out metadata!);
        }
    }

    public static string NormalizeTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return typeName;
        }

        var trimmed = typeName.Trim();
        return LegacyTypeAliases.TryGetValue(trimmed, out var mapped)
            ? mapped
            : trimmed;
    }

    public static bool TryGetByItemType(Type itemType, out CommandMetadata metadata)
    {
        if (itemType is null)
        {
            metadata = null!;
            return false;
        }

        lock (ExternalMetadataLock)
        {
            if (_externalByTypeName.Values.FirstOrDefault(x => x.ItemType == itemType) is { } external)
            {
                metadata = external;
                return true;
            }
        }

        return ByItemType.Value.TryGetValue(itemType, out metadata!);
    }

    public static void SetExternalMetadata(IEnumerable<CommandMetadata> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var map = metadata.ToDictionary(x => x.TypeName, x => x, StringComparer.Ordinal);
        lock (ExternalMetadataLock)
        {
            _externalByTypeName = new ReadOnlyDictionary<string, CommandMetadata>(map);
        }
    }

    private static IReadOnlyDictionary<string, CommandMetadata> CreateByTypeName()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var items = assembly.GetTypes()
            .Select(t => new { Type = t, Attr = t.GetCustomAttribute<CommandDefinitionAttribute>() })
            .Where(x => x.Attr is not null)
            .Select(x => new CommandMetadata
            {
                TypeName = x.Attr!.TypeName,
                ItemType = x.Type,
                CommandType = x.Attr.CommandType,
                SettingsType = x.Attr.SettingsType,
                Category = x.Attr.Category,
                IsIfCommand = x.Attr.IsIfCommand,
                IsLoopCommand = x.Attr.IsLoopCommand,
                IsEndCommand = x.Attr.IsEndCommand,
                DisplayPriority = x.Attr.DisplayPriority,
                DisplaySubPriority = x.Attr.DisplaySubPriority,
                DisplayNameJa = x.Attr.DisplayNameJa,
                DisplayNameEn = x.Attr.DisplayNameEn
            })
            .ToDictionary(x => x.TypeName, x => x, StringComparer.Ordinal);

        return new ReadOnlyDictionary<string, CommandMetadata>(items);
    }
}

