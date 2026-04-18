using System.Collections.ObjectModel;
using System.Reflection;

namespace AutoTool.Automation.Runtime.Definitions;

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
}

public static class CommandMetadataCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, CommandMetadata>> ByTypeName =
        new(CreateByTypeName);

    private static readonly Lazy<IReadOnlyDictionary<Type, CommandMetadata>> ByItemType =
        new(() => ByTypeName.Value.Values.ToDictionary(x => x.ItemType, x => x));

    public static IReadOnlyCollection<CommandMetadata> GetAll()
    {
        return ByTypeName.Value.Values.ToArray();
    }

    public static bool TryGetByTypeName(string typeName, out CommandMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            metadata = null!;
            return false;
        }

        return ByTypeName.Value.TryGetValue(typeName, out metadata!);
    }

    public static bool TryGetByItemType(Type itemType, out CommandMetadata metadata)
    {
        if (itemType is null)
        {
            metadata = null!;
            return false;
        }

        return ByItemType.Value.TryGetValue(itemType, out metadata!);
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
