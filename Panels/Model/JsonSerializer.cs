using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.Serialization;

/// <summary>
/// マクロファイルのシリアライズ/デシリアライズを行うヘルパークラス
/// </summary>
public static class MacroFileSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters = { new CommandListItemConverter() },
        WriteIndented = true
    };

    public static void SerializeToFile<T>(T obj, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var json = JsonSerializer.Serialize(obj, DefaultOptions);
        File.WriteAllText(path, json);
    }

    public static T? DeserializeFromFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }
}

/// <summary>
/// ICommandListItemのJSONコンバーター
/// </summary>
internal sealed class CommandListItemConverter : JsonConverter<ICommandListItem>
{
    public override ICommandListItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var jsonObject = doc.RootElement;
        var typeName = jsonObject.GetProperty("ItemType").GetString();

        if (string.IsNullOrEmpty(typeName))
        {
            throw new JsonException("ItemType is not found");
        }

        var targetType = CommandRegistry.GetItemType(typeName);
        if (targetType != null)
        {
            return (ICommandListItem?)JsonSerializer.Deserialize(jsonObject.GetRawText(), targetType, options)
                   ?? throw new JsonException($"Failed to deserialize {typeName}");
        }
        
        throw new NotSupportedException($"Type {typeName} is not supported");
    }

    public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
    {
        // 相互参照回避
        ClearPairReferences(value);

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    private static void ClearPairReferences(ICommandListItem value)
    {
        switch (value)
        {
            case IIfItem ifItem:
                ifItem.Pair = null;
                break;
            case IIfEndItem endIfItem:
                endIfItem.Pair = null;
                break;
            case ILoopItem loopItem:
                loopItem.Pair = null;
                break;
            case ILoopEndItem endLoopItem:
                endLoopItem.Pair = null;
                break;
        }
    }
}