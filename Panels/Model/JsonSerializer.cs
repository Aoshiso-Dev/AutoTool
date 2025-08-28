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
using MacroPanels.Model.List.Type;


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
    private readonly Dictionary<string, Type> _itemTypeMapping;

    public CommandListItemConverter()
    {
        _itemTypeMapping = new Dictionary<string, Type>
        {
            { nameof(ItemType.WaitImage), typeof(WaitImageItem) },
            { nameof(ItemType.ClickImage), typeof(ClickImageItem) },
            { nameof(ItemType.Click), typeof(ClickItem) },
            { nameof(ItemType.Hotkey), typeof(HotkeyItem) },
            { nameof(ItemType.Wait), typeof(WaitItem) },
            { nameof(ItemType.Loop), typeof(LoopItem) },
            { nameof(ItemType.EndLoop), typeof(EndLoopItem) },
            { nameof(ItemType.Break), typeof(BreakItem) },
            { nameof(ItemType.IfImageExist), typeof(IfImageExistItem) },
            { nameof(ItemType.IfImageNotExist), typeof(IfImageNotExistItem) },
            { nameof(ItemType.EndIf), typeof(EndIfItem) },
            { nameof(ItemType.IfImageExistAI), typeof(IfImageExistAIItem) },
            { nameof(ItemType.IfImageNotExistAI), typeof(IfImageNotExistAIItem) },
        };
    }

    public override ICommandListItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var jsonObject = doc.RootElement;
            var type = jsonObject.GetProperty("ItemType").GetString();

            if(type == null)
            {
                throw new JsonException("ItemType is not found");
            }

            if (_itemTypeMapping.TryGetValue(type, out Type? targetType))
            {
                return (ICommandListItem?)JsonSerializer.Deserialize(jsonObject.GetRawText(), targetType, options)
                       ?? throw new JsonException($"Failed to deserialize {type}");
            }
            throw new NotSupportedException($"Type {type} is not supported");
        }
    }

    public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
    {
        // 相互参照回避
        if (value is IIfItem ifItem)
        {
            ifItem.Pair = null;
        }
        else if (value is IEndIfItem endIfItem)
        {
            endIfItem.Pair = null;
        }
        else if (value is ILoopItem loopItem)
        {
            loopItem.Pair = null;
        }
        else if (value is IEndLoopItem endLoopItem)
        {
            endLoopItem.Pair = null;
        }

        // オブジェクトをシリアライズ
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}