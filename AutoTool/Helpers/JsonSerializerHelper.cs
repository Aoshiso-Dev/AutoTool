using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;

namespace AutoTool.Helpers
{
    /// <summary>
    /// JSON シリアライゼーション ヘルパー（DI対応）
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
            PropertyNamingPolicy = null, // CamelCaseを無効化して元の名前を保持
            PropertyNameCaseInsensitive = true, // 大文字小文字を区別しない
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // 参照保持を有効化
            ReferenceHandler = ReferenceHandler.Preserve,
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // 文字列からの数値読み取りを許可
            AllowTrailingCommas = true, // 末尾のカンマを許可
            ReadCommentHandling = JsonCommentHandling.Skip, // コメントをスキップ
            Converters =
            {
                new CommandListItemConverter(),
                new CommandListItemListConverter(),
                new CommandListItemObservableCollectionConverter(),
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
                new CommandListItemConverter(),
                new CommandListItemListConverter(),
                new CommandListItemObservableCollectionConverter(),
                new JsonStringEnumConverter()
            }
        };

        /// <summary>
        /// ログ出力ヘルパー
        /// </summary>
        private static void LogDebug(string message, params object[] args)
        {
            _logger?.LogDebug(message, args);
            System.Diagnostics.Debug.WriteLine($"[JsonSerializerHelper] {string.Format(message, args)}");
        }

        private static void LogInformation(string message, params object[] args)
        {
            _logger?.LogInformation(message, args);
            System.Diagnostics.Debug.WriteLine($"[JsonSerializerHelper] {string.Format(message, args)}");
        }

        private static void LogWarning(string message, params object[] args)
        {
            _logger?.LogWarning(message, args);
            System.Diagnostics.Debug.WriteLine($"[JsonSerializerHelper] WARNING: {string.Format(message, args)}");
        }

        private static void LogError(Exception? ex, string message, params object[] args)
        {
            _logger?.LogError(ex, message, args);
            System.Diagnostics.Debug.WriteLine($"[JsonSerializerHelper] ERROR: {string.Format(message, args)} - {ex?.Message}");
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
                throw;
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

                // JSONの構造を判定
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                LogDebug("JSON構造判定: ValueKind={ValueKind}", root.ValueKind);

                try
                {
                    // 1. 参照保持形式かチェック ($id, $values プロパティの存在)
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
                    
                    // 2. 通常の配列形式
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        LogInformation("通常の配列形式のJSONを検出: 要素数={Count}", root.GetArrayLength());
                        var result = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                        LogInformation("通常の配列形式でのデシリアライズ成功");
                        return result;
                    }

                    // 3. 通常のオブジェクト形式
                    LogInformation("通常のオブジェクト形式のJSONを検出");
                    var objResult = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                    LogInformation("通常のオブジェクト形式でのデシリアライズ成功");
                    return objResult;
                }
                catch (JsonException ex)
                {
                    LogError(ex, "JSON形式自動判定デシリアライズ失敗");
                    
                    // 最後の手段：参照保持なしで再試行
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
            LogDebug("Deserialize開始: 型={Type}, JSON長={Length}", typeof(T).Name, json.Length);
            try
            {
                var result = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                LogDebug("Deserialize完了: 型={Type}", typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, "Deserialize失敗: 型={Type}", typeof(T).Name);
                throw;
            }
        }
    }

    /// <summary>
    /// CommandListItem用カスタムJSON コンバーター
    /// </summary>
    public class CommandListItemConverter : JsonConverter<ICommandListItem>
    {
        public override ICommandListItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] Read開始: ValueKind={root.ValueKind}");

            // 要素の詳細情報をログ出力
            if (root.ValueKind == JsonValueKind.Object)
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] 全プロパティ一覧:");
                foreach (var property in root.EnumerateObject())
                {
                    var valuePreview = property.Value.ValueKind == JsonValueKind.String 
                        ? $"\"{property.Value.GetString()}\"" 
                        : property.Value.ToString();
                    System.Diagnostics.Debug.WriteLine($"  {property.Name}: {valuePreview} ({property.Value.ValueKind})");
                }
            }

            // ItemTypeを複数の方法で取得を試行
            string? itemType = null;
            JsonElement itemTypeElement = default;
            
            // 1. camelCase
            if (root.TryGetProperty("itemType", out itemTypeElement))
            {
                itemType = itemTypeElement.GetString();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] itemType (camelCase) 取得: {itemType}");
            }
            // 2. PascalCase
            else if (root.TryGetProperty("ItemType", out itemTypeElement))
            {
                itemType = itemTypeElement.GetString();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] ItemType (PascalCase) 取得: {itemType}");
            }
            // 3. 全小文字
            else if (root.TryGetProperty("itemtype", out itemTypeElement))
            {
                itemType = itemTypeElement.GetString();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] itemtype (lowercase) 取得: {itemType}");
            }

            if (string.IsNullOrEmpty(itemType))
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] itemTypeプロパティが見つからないまたは空です。型推定を試行");
                
                // プロパティから型を推定
                if (root.TryGetProperty("loopCount", out _) || root.TryGetProperty("LoopCount", out _))
                {
                    itemType = "Loop";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] LoopCountから Loop と推定");
                }
                else if (root.TryGetProperty("imagePath", out _) || root.TryGetProperty("ImagePath", out _))
                {
                    itemType = "Click_Image";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] ImagePathから Click_Image と推定");
                }
                else if (root.TryGetProperty("wait", out _) || root.TryGetProperty("Wait", out _))
                {
                    itemType = "Wait";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] Waitから Wait と推定");
                }
                else if (root.TryGetProperty("x", out _) && root.TryGetProperty("y", out _))
                {
                    itemType = "Click";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] X,Yから Click と推定");
                }
                else if (root.TryGetProperty("key", out _) || root.TryGetProperty("Key", out _))
                {
                    itemType = "Hotkey";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] Keyから Hotkey と推定");
                }
                else if (root.TryGetProperty("pair", out _) || root.TryGetProperty("Pair", out _))
                {
                    // PairがあるがLoopCountがない場合はIF系かLoop_End系
                    if (root.TryGetProperty("description", out var desc) && 
                        desc.ValueKind == JsonValueKind.String)
                    {
                        var descText = desc.GetString() ?? "";
                        if (descText.Contains("->"))
                        {
                            itemType = "IF_End"; // もしくは Loop_End
                            System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] Pairと->から IF_End と推定");
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(itemType))
                {
                    itemType = "Unknown";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] 型推定失敗、Unknown に設定");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] 最終的なitemType: {itemType}");

            // ItemTypeに基づいて適切な型を決定
            var targetType = GetItemTypeFromString(itemType);
            if (targetType == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] 未知のitemType、BasicCommandItemにフォールバック: {itemType}");
                targetType = typeof(BasicCommandItem);
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] 対象型: {targetType.Name}");

            try
            {
                var jsonString = root.GetRawText();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] JSON要素: {jsonString.Substring(0, Math.Min(300, jsonString.Length))}...");
                
                var result = (ICommandListItem?)JsonSerializer.Deserialize(jsonString, targetType, options);
                
                // ItemTypeが正しく設定されていることを確認
                if (result != null)
                {
                    if (string.IsNullOrEmpty(result.ItemType))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] ItemTypeを設定: {itemType}");
                        result.ItemType = itemType;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] 変換成功: {targetType.Name} (ItemType={result.ItemType})");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] CommandListItem変換エラー: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] スタックトレース: {ex.StackTrace}");
                
                // デシリアライズに失敗した場合、BasicCommandItemとしてフォールバック
                var basicItem = new BasicCommandItem
                {
                    ItemType = itemType ?? "Unknown",
                    Comment = "デシリアライズエラーからの復旧",
                    Description = $"元の型: {itemType}"
                };
                
                // 可能であれば基本プロパティを復元
                try
                {
                    if (root.TryGetProperty("comment", out var commentElement) || 
                        root.TryGetProperty("Comment", out commentElement))
                        basicItem.Comment = commentElement.GetString() ?? basicItem.Comment;
                    
                    if (root.TryGetProperty("isEnable", out var isEnableElement) || 
                        root.TryGetProperty("IsEnable", out isEnableElement))
                        basicItem.IsEnable = isEnableElement.GetBoolean();
                    
                    if (root.TryGetProperty("lineNumber", out var lineNumberElement) || 
                        root.TryGetProperty("LineNumber", out lineNumberElement))
                        basicItem.LineNumber = lineNumberElement.GetInt32();

                    if (root.TryGetProperty("description", out var descElement) || 
                        root.TryGetProperty("Description", out descElement))
                        basicItem.Description = descElement.GetString() ?? "";

                    System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] BasicCommandItemフォールバック成功: {itemType}");
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] BasicCommandItemプロパティ復元エラー: {ex2.Message}");
                }
                
                return basicItem;
            }
        }

        public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] Write: {value.ItemType} ({value.GetType().Name})");
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static Type? GetItemTypeFromString(string itemType)
        {
            var result = itemType switch
            {
                // AutoTool.Model.List.Class の具体的なクラスを使用
                "Wait_Image" => typeof(AutoTool.Model.List.Class.WaitImageItem),
                "Click_Image" => typeof(AutoTool.Model.List.Class.ClickImageItem),
                "Click_Image_AI" => typeof(AutoTool.Model.List.Class.ClickImageAIItem),
                "Hotkey" => typeof(AutoTool.Model.List.Class.HotkeyItem),
                "Click" => typeof(AutoTool.Model.List.Class.ClickItem),
                "Wait" => typeof(AutoTool.Model.List.Class.WaitItem),
                "Loop" => typeof(AutoTool.Model.List.Class.LoopItem),
                "Loop_End" => typeof(AutoTool.Model.List.Class.LoopEndItem),
                "Loop_Break" => typeof(AutoTool.Model.List.Class.LoopBreakItem),
                "IF_ImageExist" => typeof(AutoTool.Model.List.Class.IfImageExistItem),
                "IF_ImageNotExist" => typeof(AutoTool.Model.List.Class.IfImageNotExistItem),
                "IF_End" => typeof(AutoTool.Model.List.Class.IfEndItem),
                "IF_ImageExist_AI" => typeof(AutoTool.Model.List.Class.IfImageExistAIItem),
                "IF_ImageNotExist_AI" => typeof(AutoTool.Model.List.Class.IfImageNotExistAIItem),
                "Execute" => typeof(AutoTool.Model.List.Class.ExecuteItem),
                "SetVariable" => typeof(AutoTool.Model.List.Class.SetVariableItem),
                "SetVariable_AI" => typeof(AutoTool.Model.List.Class.SetVariableAIItem),
                "IF_Variable" => typeof(AutoTool.Model.List.Class.IfVariableItem),
                "Screenshot" => typeof(AutoTool.Model.List.Class.ScreenshotItem),
                
                // テスト用
                "Test" => typeof(BasicCommandItem),
                "Unknown" => typeof(BasicCommandItem),
                
                // フォールバック
                _ => typeof(BasicCommandItem)
            };

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] 型マッピング: {itemType} -> {result?.Name ?? "null"}");
            return result;
        }
    }

    /// <summary>
    /// List<ICommandListItem>用コンバーター
    /// </summary>
    public class CommandListItemListConverter : JsonConverter<List<ICommandListItem>>
    {
        public override List<ICommandListItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] Read開始: ValueKind={root.ValueKind}");

            // 参照保持形式の場合
            if (root.ValueKind == JsonValueKind.Object && 
                root.TryGetProperty("$values", out var valuesElement))
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemListConverter] 参照保持形式のList<ICommandListItem>を処理");
                return ProcessArrayElement(valuesElement, options);
            }

            // 通常の配列形式の場合
            if (root.ValueKind == JsonValueKind.Array)
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemListConverter] 通常の配列形式のList<ICommandListItem>を処理");
                return ProcessArrayElement(root, options);
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] 未対応の形式: {root.ValueKind}");
            return null;
        }

        private static List<ICommandListItem>? ProcessArrayElement(JsonElement arrayElement, JsonSerializerOptions options)
        {
            if (arrayElement.ValueKind != JsonValueKind.Array)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] 配列でない要素: {arrayElement.ValueKind}");
                return null;
            }

            var list = new List<ICommandListItem>();
            var converter = new CommandListItemConverter();
            var arrayLength = arrayElement.GetArrayLength();

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] 配列処理開始: 要素数={arrayLength}");

            int processed = 0;
            int success = 0;
            int errors = 0;

            foreach (var element in arrayElement.EnumerateArray())
            {
                processed++;
                try
                {
                    var elementJson = element.GetRawText();
                    using var elementDoc = JsonDocument.Parse(elementJson);
                    var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(elementJson));
                    reader.Read();

                    var item = converter.Read(ref reader, typeof(ICommandListItem), options);
                    if (item != null)
                    {
                        list.Add(item);
                        success++;
                        System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] 要素処理成功 [{processed}/{arrayLength}]: {item.ItemType}");
                    }
                    else
                    {
                        errors++;
                        System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] 要素処理結果がnull [{processed}/{arrayLength}]");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] 配列要素の処理でエラー [{processed}/{arrayLength}]: {ex.Message}");
                    // エラーが発生した要素はスキップして続行
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] 配列処理完了: 処理={processed}, 成功={success}, エラー={errors}");
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<ICommandListItem> value, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] Write開始: 要素数={value.Count}");
            writer.WriteStartArray();
            var converter = new CommandListItemConverter();
            
            foreach (var item in value)
            {
                converter.Write(writer, item, options);
            }
            
            writer.WriteEndArray();
            System.Diagnostics.Debug.WriteLine("[CommandListItemListConverter] Write完了");
        }
    }

    /// <summary>
    /// ObservableCollection<ICommandListItem>用コンバーター
    /// </summary>
    public class CommandListItemObservableCollectionConverter : JsonConverter<ObservableCollection<ICommandListItem>>
    {
        public override ObservableCollection<ICommandListItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine("[CommandListItemObservableCollectionConverter] Read開始");
            var listConverter = new CommandListItemListConverter();
            var list = listConverter.Read(ref reader, typeof(List<ICommandListItem>), options);
            
            if (list != null)
            {
                var result = new ObservableCollection<ICommandListItem>(list);
                System.Diagnostics.Debug.WriteLine($"[CommandListItemObservableCollectionConverter] ObservableCollection作成完了: 要素数={result.Count}");
                return result;
            }
            
            System.Diagnostics.Debug.WriteLine("[CommandListItemObservableCollectionConverter] Read失敗");
            return null;
        }

        public override void Write(Utf8JsonWriter writer, ObservableCollection<ICommandListItem> value, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandListItemObservableCollectionConverter] Write開始: 要素数={value.Count}");
            var listConverter = new CommandListItemListConverter();
            listConverter.Write(writer, value.ToList(), options);
            System.Diagnostics.Debug.WriteLine("[CommandListItemObservableCollectionConverter] Write完了");
        }
    }
}