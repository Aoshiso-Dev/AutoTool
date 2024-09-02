using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Reflection;
using System.IO;
using Panels.Define;
using System.Windows;

/*
public class ICommandItemConverter : JsonConverter<ICommandItem>
{
    private static readonly Dictionary<string, Type> _typeMap;

    static ICommandItemConverter()
    {
        // ICommandItemの実装クラスをマッピング
        _typeMap = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(ICommandItem).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    public override ICommandItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var typeProperty = doc.RootElement.GetProperty("Type").GetString();

            if (typeProperty != null && _typeMap.TryGetValue(typeProperty, out var type))
            {
                return (ICommandItem?)JsonSerializer.Deserialize(doc.RootElement.GetRawText(), type, options);
            }
            throw new JsonException($"Unknown type: {typeProperty}");
        }
    }

    public override void Write(Utf8JsonWriter writer, ICommandItem value, JsonSerializerOptions options)
    {
        var type = value.GetType();
        writer.WriteStartObject();

        // タイプ情報を追加
        writer.WriteString("Type", type.Name);

        // 残りのプロパティをシリアライズ
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propValue = prop.GetValue(value);
            writer.WritePropertyName(JsonNamingPolicy.CamelCase.ConvertName(prop.Name)); // プロパティ名をキャメルケースに変換
            JsonSerializer.Serialize(writer, propValue, propValue?.GetType() ?? typeof(object), options);
        }

        writer.WriteEndObject();
    }
}
*/

public class JsonSerializerHelper
{
    /*
    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new ICommandItemConverter());
        return options;
    }
    */

    public static void SerializeToFile<T>(T obj, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        //var options = CreateSerializerOptions();
        //string json = JsonSerializer.Serialize(obj, options);
        string json = JsonSerializer.Serialize(obj);
        File.WriteAllText(path, json);
    }

    public static T? DeserializeFromFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
            //throw new FileNotFoundException($"The file at {path} could not be found.");
        }

        //var options = CreateSerializerOptions();
        string json = File.ReadAllText(path);
        //return JsonSerializer.Deserialize<T>(json, options);
        return JsonSerializer.Deserialize<T>(json);
    }
}