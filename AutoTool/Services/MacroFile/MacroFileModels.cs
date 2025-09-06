using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoTool.Services.MacroFile
{
    /// <summary>
    /// マクロファイルの内部データ構造
    /// </summary>
    public class MacroFileData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = Environment.UserName;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("modifiedAt")]
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("commands")]
        public List<CommandItemData> Commands { get; set; } = new();

        [JsonPropertyName("variables")]
        public Dictionary<string, object> Variables { get; set; } = new();

        [JsonPropertyName("settings")]
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// コマンドアイテムのシリアライゼーション用データ構造
    /// </summary>
    public class CommandItemData
    {
        [JsonPropertyName("itemType")]
        public string ItemType { get; set; } = string.Empty;

        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyName("isEnable")]
        public bool IsEnable { get; set; } = true;

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("nestLevel")]
        public int NestLevel { get; set; }

        [JsonPropertyName("isInLoop")]
        public bool IsInLoop { get; set; }

        [JsonPropertyName("isInIf")]
        public bool IsInIf { get; set; }

        [JsonPropertyName("settings")]
        public Dictionary<string, object> Settings { get; set; } = new();

        [JsonPropertyName("pairLineNumber")]
        public int? PairLineNumber { get; set; }
    }

    /// <summary>
    /// レガシー形式（旧バージョン）のサポート用
    /// </summary>
    public class LegacyCommandData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}