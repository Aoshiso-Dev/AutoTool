using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Windows;
using Panels.List.Class;
using Panels.List.Interface;
using Panels.List;


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
            var type = jsonObject.GetProperty("ItemType").GetString();

            return type switch
            {
                nameof(ItemType.WaitImage) => JsonSerializer.Deserialize<WaitImageItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.ClickImage) => JsonSerializer.Deserialize<ClickImageItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Click) => JsonSerializer.Deserialize<ClickItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Hotkey) => JsonSerializer.Deserialize<HotkeyItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Wait) => JsonSerializer.Deserialize<WaitItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Loop) => JsonSerializer.Deserialize<LoopItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.EndLoop) => JsonSerializer.Deserialize<EndLoopItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                _ => throw new NotSupportedException($"Type {type} is not supported"),
            };
        }
    }

    public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
    {
        if (value is WaitImageItem waitImageItem)
        {
            JsonSerializer.Serialize(writer, waitImageItem, options);
        }
        else if (value is ClickImageItem clickImageItem)
        {
            JsonSerializer.Serialize(writer, clickImageItem, options);
        }
        else if (value is ClickItem clickItem)
        {
            JsonSerializer.Serialize(writer, clickItem, options);
        }
        else if (value is HotkeyItem hotkeyItem)
        {
            JsonSerializer.Serialize(writer, hotkeyItem, options);
        }
        else if (value is WaitItem waitItem)
        {
            JsonSerializer.Serialize(writer, waitItem, options);
        }
        else if (value is LoopItem loopItem)
        {
            JsonSerializer.Serialize(writer, loopItem, options);
        }
        else if (value is EndLoopItem endLoopItem)
        {
            JsonSerializer.Serialize(writer, endLoopItem, options);
        }
        else
        {
            throw new NotSupportedException($"Type {value.GetType().Name} is not supported");
        }
    }

    /*
    public override CommandListItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var jsonObject = doc.RootElement;
            var type = jsonObject.GetProperty("ItemType").GetString();

            return type switch
            {
                nameof(ItemType.WaitImage) => JsonSerializer.Deserialize<WaitItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.ClickImage) => JsonSerializer.Deserialize<ClickImageItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Click) => JsonSerializer.Deserialize<ClickItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Hotkey) => JsonSerializer.Deserialize<HotkeyItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Wait) => JsonSerializer.Deserialize<WaitItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.Loop) => JsonSerializer.Deserialize<LoopItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                nameof(ItemType.EndLoop) => JsonSerializer.Deserialize<EndLoopItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
                _ => JsonSerializer.Deserialize<CommandListItem>(jsonObject.GetRawText(), options) ?? new CommandListItem(),
            };
        }
    }
    */
    /*
    public override void Write(Utf8JsonWriter writer, CommandListItem value, JsonSerializerOptions options)
    {
        switch(value.ItemType)
        {
            case nameof(ItemType.WaitImage):
                JsonSerializer.Serialize<WaitImageItem>(writer, (WaitImageItem)value, options);
                break;
            case nameof(ItemType.ClickImage):
                JsonSerializer.Serialize(writer, (ClickImageItem)value, options);
                break;
            case nameof(ItemType.Click):
                JsonSerializer.Serialize(writer, (ClickItem)value, options);
                break;
            case nameof(ItemType.Hotkey):
                JsonSerializer.Serialize(writer, (HotkeyItem)value, options);
                break;
            case nameof(ItemType.Wait):
                JsonSerializer.Serialize<WaitItem>(writer, (WaitItem)value, options);
                break;
            case nameof(ItemType.Loop):
                JsonSerializer.Serialize(writer, (LoopItem)value, options);
                break;
            case nameof(ItemType.EndLoop):
                JsonSerializer.Serialize(writer, (EndLoopItem)value, options);
                break;
            default:
                JsonSerializer.Serialize(writer, value, options);
                break;
        }
    }
    */
}