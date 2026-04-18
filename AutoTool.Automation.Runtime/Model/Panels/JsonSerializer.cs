using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Serialization;

namespace AutoTool.Automation.Runtime.Serialization;

/// <summary>
/// マクロファイルのシリアライズ/デシリアライズを行うインターフェース
/// </summary>
public interface IMacroFileSerializer
{
    void SerializeToFile<T>(T obj, string path);
    T? DeserializeFromFile<T>(string path);
}

/// <summary>
/// マクロファイルのシリアライズ/デシリアライズ実装
/// </summary>
public sealed class MacroFileSerializer : IMacroFileSerializer
{
    private static readonly IReadOnlyDictionary<string, string> LegacyItemTypeMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ClickImage"] = CommandTypeNames.ClickImage,
            ["FindImage"] = CommandTypeNames.FindImage,
            ["FindText"] = CommandTypeNames.FindText,
            ["ClickImageAI"] = CommandTypeNames.ClickImageAI,
            ["WaitImage"] = CommandTypeNames.WaitImage,
            ["EndLoop"] = CommandTypeNames.LoopEnd,
            ["Break"] = CommandTypeNames.LoopBreak,
            ["BreakLoop"] = CommandTypeNames.LoopBreak,
            ["EndIf"] = CommandTypeNames.IfEnd,
            ["IfImageExist"] = CommandTypeNames.IfImageExist,
            ["IfImageNotExist"] = CommandTypeNames.IfImageNotExist,
            ["IfTextExist"] = CommandTypeNames.IfTextExist,
            ["IfTextNotExist"] = CommandTypeNames.IfTextNotExist,
            ["IfImageExistAI"] = CommandTypeNames.IfImageExistAI,
            ["IfImageNotExistAI"] = CommandTypeNames.IfImageNotExistAI,
            ["IfVariable"] = CommandTypeNames.IfVariable,
            ["SetVariableAI"] = CommandTypeNames.SetVariableAI,
            ["SetVariableOCR"] = CommandTypeNames.SetVariableOCR
        };

    private static readonly JsonSerializerOptions LegacyValueDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private readonly JsonSerializerOptions _options;

    public MacroFileSerializer()
    {
        _options = AutoToolJsonOptionsFactory.CreateMacroSerializerOptions();
    }

    public void SerializeToFile<T>(T obj, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        PrepareForSerialization(obj);
        var json = JsonSerializer.Serialize(obj, _options);
        File.WriteAllText(path, json);
    }

    public T? DeserializeFromFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);

        if (TryDeserializeLegacy<T>(json, out var legacyResult))
        {
            return legacyResult;
        }

        return JsonSerializer.Deserialize<T>(json, _options);
    }

    private static bool TryDeserializeLegacy<T>(string json, out T? result)
    {
        result = default;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return false;
        }

        if (node is not JsonObject root || root["$values"] is not JsonArray values)
        {
            return false;
        }

        var items = new ObservableCollection<ICommandListItem>();

        foreach (var entry in values.OfType<JsonObject>())
        {
            entry.Remove("$id");

            var rawItemType = entry["ItemType"]?.GetValue<string>() ?? string.Empty;
            var itemType = MapLegacyItemType(rawItemType);

            if (string.IsNullOrWhiteSpace(itemType) || !CommandMetadataCatalog.TryGetByTypeName(itemType, out var metadata))
            {
                throw new InvalidDataException($"不明な ItemType: {rawItemType}");
            }

            if (Activator.CreateInstance(metadata.ItemType) is not ICommandListItem instance)
            {
                throw new InvalidDataException($"アイテム作成失敗: {itemType}");
            }

            HydrateProperties(instance, metadata.ItemType, entry, itemType);
            items.Add(instance);
        }

        if (typeof(T).IsAssignableFrom(typeof(ObservableCollection<ICommandListItem>)))
        {
            result = (T)(object)items;
            return true;
        }

        return false;
    }

    private static void HydrateProperties(ICommandListItem instance, Type concreteType, JsonObject source, string itemType)
    {
        instance.ItemType = itemType;

        foreach (var property in concreteType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
            {
                continue;
            }

            if (string.Equals(property.Name, nameof(ICommandListItem.ItemType), StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(property.Name, "Pair", StringComparison.Ordinal))
            {
                continue;
            }

            if (!source.TryGetPropertyValue(property.Name, out var node) || node is null)
            {
                continue;
            }

            try
            {
                var value = JsonSerializer.Deserialize(node.ToJsonString(), property.PropertyType, LegacyValueDeserializeOptions);
                property.SetValue(instance, value);
            }
            catch
            {
                // 互換読み込みのため、変換不能な値は無視して既定値を維持する。
            }
        }
    }

    private static string MapLegacyItemType(string itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
        {
            return itemType;
        }

        return LegacyItemTypeMap.TryGetValue(itemType, out var mapped) ? mapped : itemType;
    }

    private static void PrepareForSerialization<T>(T obj)
    {
        if (obj is not IEnumerable<ICommandListItem> items)
        {
            return;
        }

        foreach (var item in items)
        {
            ClearPairReferences(item);
        }
    }

    private static void ClearPairReferences(ICommandListItem value)
    {
        var clearAction = value switch
        {
            IIfItem ifItem => () => ifItem.Pair = null,
            IIfEndItem endIfItem => () => endIfItem.Pair = null,
            ILoopItem loopItem => () => loopItem.Pair = null,
            ILoopEndItem endLoopItem => () => endLoopItem.Pair = null,
            _ => (Action?)null
        };

        clearAction?.Invoke();
    }
}
