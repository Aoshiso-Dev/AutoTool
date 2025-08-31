using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Windows;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;

public class JsonSerializerHelper
{
    public static void SerializeToFile<T>(T obj, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters = { new CommandListItemConverter() },
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(obj, options);
        File.WriteAllText(path, json);
    }

    public static T? DeserializeFromFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters = { new CommandListItemConverter() },
            WriteIndented = true
        };

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, options);
    }
}

internal class CommandListItemConverter : JsonConverter<ICommandListItem>
{
    public override ICommandListItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var jsonObject = doc.RootElement;
            var typeName = jsonObject.GetProperty("ItemType").GetString();

            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonException("ItemType is not found");
            }

            // CommandRegistryを使用して動的に型を取得
            var targetType = CommandRegistry.GetItemType(typeName);
            if (targetType != null)
            {
                return (ICommandListItem?)JsonSerializer.Deserialize(jsonObject.GetRawText(), targetType, options)
                       ?? throw new JsonException($"Failed to deserialize {typeName}");
            }
            
            throw new NotSupportedException($"Type {typeName} is not supported");
        }
    }

    public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
    {
        // 相互参照回避
        if (value is IIfItem ifItem)
        {
            ifItem.Pair = null;
        }
        else if (value is IIfEndItem endIfItem)
        {
            endIfItem.Pair = null;
        }
        else if (value is ILoopItem loopItem)
        {
            loopItem.Pair = null;
        }
        else if (value is ILoopEndItem endLoopItem)
        {
            endLoopItem.Pair = null;
        }

        // オブジェクトをシリアライズ
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}