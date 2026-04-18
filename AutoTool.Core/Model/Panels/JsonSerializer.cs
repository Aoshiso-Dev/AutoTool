using System;
using System.IO;
using System.Text.Json;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Serialization;

namespace AutoTool.Panels.Serialization;

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
        return JsonSerializer.Deserialize<T>(json, _options);
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
