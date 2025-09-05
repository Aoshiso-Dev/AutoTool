using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition; // UniversalCommandItem用
using System.Windows.Input;
using System.Linq; // 追加

namespace AutoTool.Helpers
{
    /// <summary>
    /// JSON シリアライザー ヘルパー（DI対応）
    /// </summary>
    public static class JsonSerializerHelper
    {
        // 静的ロガー（DIから設定）
        private static ILogger? _logger;

        /// <summary>
        /// DIからロガーを設定
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // 参照保持を有効化
            ReferenceHandler = ReferenceHandler.Preserve,
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // 文字列からの数値読み取りを許可
            AllowTrailingCommas = true, // 末尾のカンマを許可
            ReadCommentHandling = JsonCommentHandling.Skip, // コメントをスキップ
            Converters =
            {
                new MouseButtonEnumConverter(), // 追加: マウスボタン用カスタムコンバーター
                new UniversalCommandItemConverter(),
                new UniversalCommandItemListConverter(),
                new UniversalCommandItemObservableCollectionConverter(),
                new JsonStringEnumConverter()
            }
        };

        // 参照保持なしのオプション（従来の形式用）
        private static readonly JsonSerializerOptions OptionsWithoutReferences = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null, // CamelCaseを無効化して元の名前を保持
            PropertyNameCaseInsensitive = true, // 大文字小文字を区別しない
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // 文字列からの数値読み取りを許可
            AllowTrailingCommas = true, // 末尾のカンマを許可
            ReadCommentHandling = JsonCommentHandling.Skip, // コメントをスキップ
            Converters =
            {
                new MouseButtonEnumConverter(), // 追加: マウスボタン用カスタムコンバーター
                new UniversalCommandItemConverter(),
                new UniversalCommandItemListConverter(),
                new UniversalCommandItemObservableCollectionConverter(),
                new JsonStringEnumConverter()
            }
        };

        // ===== ログヘルパ（string.Format を使わず例外を防ぐ） =====
        private static string CombineMessage(string message, object[] args)
        {
            if (args == null || args.Length == 0) return message;
            // ストラクチャードログの {Name} プレースホルダーはそのまま残し、末尾に値を列挙
            var argList = string.Join(", ", args.Select((a, i) => $"arg{i}={a}"));
            return $"{message} | {argList}"; // 安全な連結
        }

        private static void LogDebug(string message, params object[] args)
        {
            _logger?.LogDebug(message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] " + CombineMessage(message, args));
        }

        private static void LogInformation(string message, params object[] args)
        {
            _logger?.LogInformation(message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] " + CombineMessage(message, args));
        }

        private static void LogWarning(string message, params object[] args)
        {
            _logger?.LogWarning(message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] WARNING: " + CombineMessage(message, args));
        }

        private static void LogError(Exception? ex, string message, params object[] args)
        {
            _logger?.LogError(ex, message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] ERROR: " + CombineMessage(message, args) + (ex != null ? $" - {ex.Message}" : string.Empty));
        }

        /// <summary>
        /// オブジェクトをファイルにシリアライズ
        /// </summary>
        public static void SerializeToFile<T>(T obj, string filePath)
        {
            LogDebug("SerializeToFile開始: ファイルパス={FilePath}, 型={Type}", filePath, typeof(T).Name);
            
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    LogDebug("ディレクトリ作成: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // 参照保持なしでシリアライズ（互換性のため）
                LogDebug("JSONシリアライズ開始（参照保持なし）");
                var json = JsonSerializer.Serialize(obj, OptionsWithoutReferences);
                
                LogDebug("JSON文字列長: {Length}文字", json.Length);
                LogDebug("JSON先頭100文字: {JsonStart}", json.Substring(0, Math.Min(100, json.Length)));
                
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                LogInformation("ファイル保存完了: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                LogError(ex, "ファイル保存失敗: {FilePath}", filePath);
                throw new InvalidOperationException($"ファイル保存に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ファイルからデシリアライズ（複数の形式を試行）
        /// </summary>
        public static T? DeserializeFromFile<T>(string filePath)
        {
            LogDebug("DeserializeFromFile開始: ファイルパス={FilePath}, 型={Type}", filePath, typeof(T).Name);
            
            if (!File.Exists(filePath))
            {
                LogWarning("ファイルが見つかりません: {FilePath}", filePath);
                return default;
            }

            try
            {
                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                LogDebug("ファイル読み込み完了: 文字数={Length}", json.Length);
                LogDebug("JSON先頭200文字: {JsonStart}", json.Substring(0, Math.Min(200, json.Length)));

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                LogDebug("JSON構造判定: ValueKind={ValueKind}", root.ValueKind);

                try
                {
                    if (root.ValueKind == JsonValueKind.Object && 
                        root.TryGetProperty("$id", out var idProp) && 
                        root.TryGetProperty("$values", out var valuesProp))
                    {
                        LogInformation("参照保持形式のJSONを検出: $id={Id}, $values要素数={Count}", 
                            idProp.GetString(), 
                            valuesProp.ValueKind == JsonValueKind.Array ? valuesProp.GetArrayLength() : 0);
                        
                        var result = JsonSerializer.Deserialize<T>(json, Options);
                        LogInformation("参照保持形式でのデシリアライズ成功");
                        return result;
                    }
                    
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        LogInformation("通常の配列形式のJSONを検出: 要素数={Count}", root.GetArrayLength());
                        var result = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                        LogInformation("通常の配列形式でのデシリアライズ成功");
                        return result;
                    }

                    LogInformation("通常のオブジェクト形式のJSONを検出");
                    var objResult = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                    LogInformation("通常のオブジェクト形式でのデシリアライズ成功");
                    return objResult;
                }
                catch (JsonException ex)
                {
                    LogError(ex, "JSON形式自動判定デシリアライズ失敗");
                    try
                    {
                        LogDebug("フォールバック: 参照保持なしで再試行");
                        var fallbackResult = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                        LogInformation("フォールバックデシリアライズ成功");
                        return fallbackResult;
                    }
                    catch (JsonException ex2)
                    {
                        LogError(ex2, "フォールバックデシリアライズ失敗");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "ファイル読み込み処理中にエラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// オブジェクトをJSON文字列にシリアライズ
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            LogDebug("Serialize開始: 型={Type}", typeof(T).Name);
            try
            {
                var result = JsonSerializer.Serialize(obj, OptionsWithoutReferences);
                LogDebug("Serialize完了: 文字数={Length}", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, "Serialize失敗: 型={Type}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// JSON文字列からデシリアライズ
        /// </summary>
        public static T? Deserialize<T>(string json)
        {
            LogDebug("Deserialize開始: 型={Type}", typeof(T).Name);
            try
            {
                var result = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                LogDebug("Deserialize完了");
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, "Deserialize失敗: 型={Type}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// ItemTypeから対応するTypeを取得する辞書
        /// </summary>
        private static readonly Dictionary<string, Type> ItemTypeToTypeMap = new()
        {
            { "Click", typeof(UniversalCommandItem) },
            { "Wait_Image", typeof(UniversalCommandItem) },
            { "Click_Image", typeof(UniversalCommandItem) },
            { "Click_Image_AI", typeof(UniversalCommandItem) },
            { "Hotkey", typeof(UniversalCommandItem) },
            { "Wait", typeof(UniversalCommandItem) },
            { "Loop", typeof(UniversalCommandItem) },
            { "Loop_End", typeof(UniversalCommandItem) },
            { "Loop_Break", typeof(UniversalCommandItem) },
            { "IF_ImageExist", typeof(UniversalCommandItem) },
            { "IF_ImageNotExist", typeof(UniversalCommandItem) },
            { "IF_End", typeof(UniversalCommandItem) },
            { "IF_ImageExist_AI", typeof(UniversalCommandItem) },
            { "IF_ImageNotExist_AI", typeof(UniversalCommandItem) },
            { "Execute", typeof(UniversalCommandItem) },
            { "SetVariable", typeof(UniversalCommandItem) },
            { "SetVariable_AI", typeof(UniversalCommandItem) },
            { "IF_Variable", typeof(UniversalCommandItem) },
            { "Screenshot", typeof(UniversalCommandItem) },
            // フォールバック用
            { "", typeof(UniversalCommandItem) }
        };

        /// <summary>
        /// ItemTypeから実際のTypeを取得
        /// </summary>
        public static Type GetTypeFromItemType(string itemType)
        {
            return ItemTypeToTypeMap.TryGetValue(itemType, out var type) ? type : typeof(UniversalCommandItem);
        }
    }

    #region カスタムコンバーター

    /// <summary>
    /// マウスボタン用のカスタムコンバーター
    /// </summary>
    public class MouseButtonEnumConverter : JsonConverter<MouseButton>
    {
        public override MouseButton Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (Enum.TryParse<MouseButton>(stringValue, true, out var result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                var intValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(MouseButton), intValue))
                {
                    return (MouseButton)intValue;
                }
            }

            return MouseButton.Left; // デフォルト値
        }

        public override void Write(Utf8JsonWriter writer, MouseButton value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// UniversalCommandItem用のカスタムコンバーター
    /// </summary>
    public class UniversalCommandItemConverter : JsonConverter<UniversalCommandItem>
    {
        public override UniversalCommandItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // ItemTypeプロパティを取得
            var itemType = root.TryGetProperty("ItemType", out var itemTypeProp) 
                ? itemTypeProp.GetString() ?? ""
                : "";

            // UniversalCommandItemを作成
            var item = DirectCommandRegistry.CreateUniversalItem(itemType);
            if (item == null)
            {
                item = new UniversalCommandItem { ItemType = itemType };
            }

            // プロパティを復元
            if (root.TryGetProperty("IsEnable", out var isEnableProp))
                item.IsEnable = isEnableProp.GetBoolean();
            if (root.TryGetProperty("LineNumber", out var lineNumberProp))
                item.LineNumber = lineNumberProp.GetInt32();
            if (root.TryGetProperty("Comment", out var commentProp))
                item.Comment = commentProp.GetString() ?? "";
            // 他のプロパティも同様に復元...

            return item;
        }

        public override void Write(Utf8JsonWriter writer, UniversalCommandItem value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    /// <summary>
    /// UniversalCommandItemのList用コンバーター
    /// </summary>
    public class UniversalCommandItemListConverter : JsonConverter<List<UniversalCommandItem>>
    {
        public override List<UniversalCommandItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            var list = new List<UniversalCommandItem>();
            var itemConverter = new UniversalCommandItemConverter();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return list;
                }

                var item = itemConverter.Read(ref reader, typeof(UniversalCommandItem), options);
                if (item != null)
                {
                    list.Add(item);
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, List<UniversalCommandItem> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            var itemConverter = new UniversalCommandItemConverter();
            foreach (var item in value)
            {
                itemConverter.Write(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// UniversalCommandItemのObservableCollection用コンバーター
    /// </summary>
    public class UniversalCommandItemObservableCollectionConverter : JsonConverter<ObservableCollection<UniversalCommandItem>>
    {
        public override ObservableCollection<UniversalCommandItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var listConverter = new UniversalCommandItemListConverter();
            var list = listConverter.Read(ref reader, typeof(List<UniversalCommandItem>), options);
            return list != null ? new ObservableCollection<UniversalCommandItem>(list) : null;
        }

        public override void Write(Utf8JsonWriter writer, ObservableCollection<UniversalCommandItem> value, JsonSerializerOptions options)
        {
            var listConverter = new UniversalCommandItemListConverter();
            listConverter.Write(writer, value.ToList(), options);
        }
    }

    #endregion
}