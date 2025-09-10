using System.Text.Json;
using System.Text.Json.Serialization;


namespace AutoTool.Core.Serialization;


public static class JsonOptions
{
    public static JsonSerializerOptions Create(bool indented = true)
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return o;
    }
}