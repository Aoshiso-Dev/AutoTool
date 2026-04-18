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
        PreserveLegacyPathLikeValuesIfNeeded(obj, path);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        PrepareForSerialization(obj);
        var json = obj is IEnumerable<ICommandListItem> items
            ? SerializeCommandItems(items)
            : JsonSerializer.Serialize(obj, _options);
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

        if (TryDeserializeModernCommandItems<T>(json, out var modernResult))
        {
            return modernResult;
        }

        return JsonSerializer.Deserialize<T>(json, _options);
    }

    private string SerializeCommandItems(IEnumerable<ICommandListItem> items)
    {
        var array = new JsonArray();

        foreach (var item in items)
        {
            var node = JsonSerializer.SerializeToNode(item, item.GetType(), _options);
            if (node is not null)
            {
                array.Add(node);
            }
        }

        return array.ToJsonString(_options);
    }

    private bool TryDeserializeModernCommandItems<T>(string json, out T? result)
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

        if (node is not JsonArray values)
        {
            return false;
        }

        var items = new ObservableCollection<ICommandListItem>();

        foreach (var entry in values.OfType<JsonObject>())
        {
            var rawItemType = entry["ItemType"]?.GetValue<string>() ?? string.Empty;
            var itemType = MapLegacyItemType(rawItemType);

            if (string.IsNullOrWhiteSpace(itemType) || !CommandMetadataCatalog.TryGetByTypeName(itemType, out var metadata))
            {
                throw new InvalidDataException($"不明な ItemType: {rawItemType}");
            }

            var deserialized = JsonSerializer.Deserialize(entry.ToJsonString(), metadata.ItemType, _options);
            if (deserialized is not ICommandListItem commandItem)
            {
                throw new InvalidDataException($"アイテム復元失敗: {itemType}");
            }

            if (string.IsNullOrWhiteSpace(commandItem.ItemType) || string.Equals(commandItem.ItemType, "None", StringComparison.Ordinal))
            {
                commandItem.ItemType = itemType;
            }

            items.Add(commandItem);
        }

        if (typeof(T).IsAssignableFrom(typeof(ObservableCollection<ICommandListItem>)))
        {
            result = (T)(object)items;
            return true;
        }

        return false;
    }

    private void PreserveLegacyPathLikeValuesIfNeeded<T>(T obj, string path)
    {
        if (obj is not IEnumerable<ICommandListItem> newItems || !File.Exists(path))
        {
            return;
        }

        if (!TryGetLegacyItemsNode(path, out var legacyItemsNode))
        {
            return;
        }

        var newItemList = newItems.ToList();
        var count = Math.Min(newItemList.Count, legacyItemsNode.Count);

        for (var i = 0; i < count; i++)
        {
            var currentItem = newItemList[i];
            if (legacyItemsNode[i] is not JsonObject legacyItem)
            {
                continue;
            }

            var legacyItemType = legacyItem["ItemType"]?.GetValue<string>() ?? string.Empty;
            var mappedLegacyItemType = MapLegacyItemType(legacyItemType);

            if (!string.Equals(currentItem.ItemType, mappedLegacyItemType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            CopyMissingPathLikePropertiesFromLegacyJson(legacyItem, currentItem);
        }
    }

    private static bool TryGetLegacyItemsNode(string path, out JsonArray items)
    {
        items = new JsonArray();

        JsonNode? rootNode;
        try
        {
            rootNode = JsonNode.Parse(File.ReadAllText(path));
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }

        if (rootNode is not JsonObject rootObject || rootObject["$values"] is not JsonArray values)
        {
            return false;
        }

        items = values;
        return true;
    }

    private static void CopyMissingPathLikePropertiesFromLegacyJson(JsonObject source, ICommandListItem target)
    {
        var targetType = target.GetType();

        foreach (var targetProperty in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!targetProperty.CanRead || !targetProperty.CanWrite || targetProperty.PropertyType != typeof(string))
            {
                continue;
            }

            if (!IsPathLikeProperty(targetProperty.Name))
            {
                continue;
            }

            var targetValue = targetProperty.GetValue(target) as string;
            if (!string.IsNullOrWhiteSpace(targetValue))
            {
                continue;
            }

            if (!source.TryGetPropertyValue(targetProperty.Name, out var sourceNode) || sourceNode is null)
            {
                continue;
            }

            if (sourceNode is not JsonValue sourceValueNode)
            {
                continue;
            }

            string? sourceValue;
            try
            {
                sourceValue = sourceValueNode.GetValue<string>();
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(sourceValue))
            {
                continue;
            }

            targetProperty.SetValue(target, sourceValue);
        }
    }

    private static bool IsPathLikeProperty(string propertyName)
    {
        return propertyName.EndsWith("Path", StringComparison.OrdinalIgnoreCase)
               || propertyName.EndsWith("Directory", StringComparison.OrdinalIgnoreCase);
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
