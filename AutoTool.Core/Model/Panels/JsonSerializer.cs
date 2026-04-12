using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Model.CommandDefinition;

namespace AutoTool.Panels.Serialization;

/// <summary>
/// マクロファイルのシリアライズ/デシリアライズを行うヘルパークラス
/// </summary>
public interface IMacroFileSerializer
{
    void SerializeToFile<T>(T obj, string path);
    T? DeserializeFromFile<T>(string path);
}

/// <summary>
/// マクロファイルのシリアライズ/デシリアライズを行うサービス
/// </summary>
public sealed class MacroFileSerializer : IMacroFileSerializer
{
    private readonly JsonSerializerOptions _options;

    public MacroFileSerializer(ICommandDefinitionProvider definitionProvider)
    {
        _options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true
        };
        _options.Converters.Add(new CommandListItemConverter(definitionProvider));
    }

    public void SerializeToFile<T>(T obj, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

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
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}

/// <summary>
/// ICommandListItemのJSONコンバーター
/// </summary>
internal sealed class CommandListItemConverter : JsonConverter<ICommandListItem>
{
    private readonly ICommandDefinitionProvider _definitionProvider;

    public CommandListItemConverter(ICommandDefinitionProvider definitionProvider)
    {
        _definitionProvider = definitionProvider ?? throw new ArgumentNullException(nameof(definitionProvider));
    }

    public override ICommandListItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var jsonObject = doc.RootElement;
        var typeName = jsonObject.GetProperty("ItemType").GetString();

        if (string.IsNullOrEmpty(typeName))
        {
            throw new JsonException("ItemType is not found");
        }

        var targetType = _definitionProvider.GetItemType(typeName);
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

