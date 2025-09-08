using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Shared;
using AutoTool.Command.Definition;
using AutoTool.Services.UI;

namespace AutoTool.Services.MacroFile
{
    /// <summary>
    /// マクロファイルの読み書きサービス実装
    /// </summary>
    public class MacroFileService : IMacroFileService
    {
        private readonly ILogger<MacroFileService> _logger;
        private readonly RecentFileService _recentFileService;

        private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };

        public MacroFileService(
            ILogger<MacroFileService> logger,
            RecentFileService recentFileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
        }

        /// <summary>
        /// マクロファイルを読み込み
        /// </summary>
        public async Task<MacroFileResult> LoadMacroFileAsync(string filePath)
        {
            var result = new MacroFileResult { FilePath = filePath };

            try
            {
                _logger.LogInformation("マクロファイル読み込み開始: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "指定されたファイルが見つかりません";
                    _logger.LogError("ファイルが見つかりません: {FilePath}", filePath);
                    return result;
                }

                if (!IsSupportedFileFormat(filePath))
                {
                    result.ErrorMessage = "サポートされていないファイル形式です";
                    _logger.LogError("サポートされていないファイル形式: {FilePath}", filePath);
                    return result;
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                _logger.LogDebug("ファイル内容読み込み完了: {Size} bytes", jsonContent.Length);

                var parseResult = await ParseMacroFileAsync(jsonContent);
                if (!parseResult.Success)
                {
                    result.ErrorMessage = parseResult.ErrorMessage;
                    return result;
                }

                result.Items = parseResult.Items;
                result.Metadata = parseResult.Metadata;
                result.Success = true;

                // 最近開いたファイルに追加
                _recentFileService.AddRecentFile(filePath);

                _logger.LogInformation("マクロファイル読み込み完了: {FilePath}, Commands={Count}",
                    filePath, result.Items.Count);

                return result;
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = "JSONファイルの形式が無効です";
                _logger.LogError(ex, "JSON解析エラー: {FilePath}", filePath);
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"ファイル読み込みエラー: {ex.Message}";
                _logger.LogError(ex, "ファイル読み込み中にエラー: {FilePath}", filePath);
                return result;
            }
        }

        /// <summary>
        /// マクロファイルを保存
        /// </summary>
        public async Task<bool> SaveMacroFileAsync(string filePath, IEnumerable<UniversalCommandItem> items, MacroFileMetadata? metadata = null)
        {
            try
            {
                _logger.LogInformation("マクロファイル保存開始: {FilePath}", filePath);

                var itemList = items.ToList();
                var macroData = await CreateMacroFileDataAsync(filePath, itemList, metadata);

                var jsonContent = JsonSerializer.Serialize(macroData, DefaultSerializerOptions);
                await File.WriteAllTextAsync(filePath, jsonContent);

                // 最近開いたファイルに追加
                _recentFileService.AddRecentFile(filePath);

                _logger.LogInformation("マクロファイル保存完了: {FilePath}, Commands={Count}",
                    filePath, itemList.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロファイル保存中にエラー: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// ファイルダイアログを表示してマクロファイルを読み込み
        /// </summary>
        public async Task<MacroFileResult> ShowLoadFileDialogAsync()
        {
            var result = new MacroFileResult();

            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "マクロファイルを開く",
                    Filter = "AutoTool マクロファイル (*.atmacro)|*.atmacro|すべてのファイル (*.*)|*.*",
                    DefaultExt = ".atmacro",
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    result.ErrorMessage = "ファイル選択がキャンセルされました";
                    return result;
                }

                return await LoadMacroFileAsync(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"ファイルダイアログエラー: {ex.Message}";
                _logger.LogError(ex, "ファイルダイアログでエラー");
                return result;
            }
        }

        /// <summary>
        /// ファイルダイアログを表示してマクロファイルを保存
        /// </summary>
        public async Task<MacroFileSaveResult> ShowSaveFileDialogAsync(IEnumerable<UniversalCommandItem> items, string currentFileName = "新規ファイル")
        {
            var result = new MacroFileSaveResult();

            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "マクロファイルを保存",
                    Filter = "AutoTool マクロファイル (*.atmacro)|*.atmacro",
                    DefaultExt = ".atmacro",
                    FileName = currentFileName.EndsWith(".atmacro") ? currentFileName : $"{currentFileName}.atmacro",
                    AddExtension = true
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    result.ErrorMessage = "ファイル保存がキャンセルされました";
                    return result;
                }

                var success = await SaveMacroFileAsync(saveFileDialog.FileName, items);
                result.Success = success;
                result.FilePath = saveFileDialog.FileName;

                if (!success)
                {
                    result.ErrorMessage = "ファイル保存に失敗しました";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"ファイルダイアログエラー: {ex.Message}";
                _logger.LogError(ex, "保存ファイルダイアログでエラー");
                return result;
            }
        }

        /// <summary>
        /// マクロファイルのメタデータを検証
        /// </summary>
        public async Task<MacroFileMetadata?> ValidateMacroFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var jsonContent = await File.ReadAllTextAsync(filePath);
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                var metadata = new MacroFileMetadata();

                if (root.TryGetProperty("version", out var versionElement))
                    metadata.Version = versionElement.GetString() ?? "1.0";

                if (root.TryGetProperty("name", out var nameElement))
                    metadata.Name = nameElement.GetString() ?? Path.GetFileNameWithoutExtension(filePath);

                if (root.TryGetProperty("description", out var descElement))
                    metadata.Description = descElement.GetString() ?? string.Empty;

                if (root.TryGetProperty("author", out var authorElement))
                    metadata.Author = authorElement.GetString() ?? Environment.UserName;

                if (root.TryGetProperty("createdAt", out var createdElement) && 
                    createdElement.TryGetDateTime(out var createdAt))
                    metadata.CreatedAt = createdAt;

                if (root.TryGetProperty("modifiedAt", out var modifiedElement) && 
                    modifiedElement.TryGetDateTime(out var modifiedAt))
                    metadata.ModifiedAt = modifiedAt;

                if (root.TryGetProperty("commands", out var commandsElement) && 
                    commandsElement.ValueKind == JsonValueKind.Array)
                    metadata.CommandCount = commandsElement.GetArrayLength();

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "メタデータ検証中にエラー: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// サポートされるファイル形式かどうかを確認
        /// </summary>
        public bool IsSupportedFileFormat(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension is ".atmacro";
        }

        #region Private Methods

        /// <summary>
        /// マクロファイルをパース
        /// </summary>
        private async Task<MacroFileResult> ParseMacroFileAsync(string jsonContent)
        {
            var result = new MacroFileResult();

            try
            {
                // 行番号カウンターをリセット
                _currentLineCounter = 1;

                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // メタデータを取得
                result.Metadata = ExtractMetadata(root);

                // コマンドを取得
                JsonElement commandsElement;
                if (root.TryGetProperty("commands", out commandsElement) && commandsElement.ValueKind == JsonValueKind.Array)
                {
                    // 新形式: マクロファイル形式
                    foreach (var commandElement in commandsElement.EnumerateArray())
                    {
                        var item = await ParseCommandElementAsync(commandElement);
                        if (item != null)
                        {
                            result.Items.Add(item);
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    // 旧形式: コマンド配列の直接形式
                    foreach (var commandElement in root.EnumerateArray())
                    {
                        var item = await ParseLegacyCommandElementAsync(commandElement);
                        if (item != null)
                        {
                            result.Items.Add(item);
                        }
                    }
                }

                // 行番号でソート
                result.Items = result.Items.OrderBy(i => i.LineNumber).ToList();

                // Pairの関係を復元
                RestorePairRelationships(result.Items);

                result.Success = true;
                _logger.LogInformation("マクロファイルパース完了: {Count}個のコマンド", result.Items.Count);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"パースエラー: {ex.Message}";
                _logger.LogError(ex, "マクロファイルのパースに失敗");
                return result;
            }
        }

        // パース中の行番号カウンター
        private int _currentLineCounter = 1;

        /// <summary>
        /// 自動行番号取得
        /// </summary>
        private int GetAutoLineNumber()
        {
            return _currentLineCounter++;
        }

        /// <summary>
        /// メタデータを抽出
        /// </summary>
        private MacroFileMetadata ExtractMetadata(JsonElement root)
        {
            var metadata = new MacroFileMetadata();

            if (root.TryGetProperty("version", out var versionElement))
                metadata.Version = versionElement.GetString() ?? "1.0";

            if (root.TryGetProperty("name", out var nameElement))
                metadata.Name = nameElement.GetString() ?? string.Empty;

            if (root.TryGetProperty("description", out var descElement))
                metadata.Description = descElement.GetString() ?? string.Empty;

            if (root.TryGetProperty("author", out var authorElement))
                metadata.Author = authorElement.GetString() ?? Environment.UserName;

            if (root.TryGetProperty("createdAt", out var createdElement) && 
                createdElement.TryGetDateTime(out var createdAt))
                metadata.CreatedAt = createdAt;

            if (root.TryGetProperty("modifiedAt", out var modifiedElement) && 
                modifiedElement.TryGetDateTime(out var modifiedAt))
                metadata.ModifiedAt = modifiedAt;

            return metadata;
        }

        /// <summary>
        /// JSONエレメントからUniversalCommandItemを作成
        /// </summary>
        private async Task<UniversalCommandItem?> ParseCommandElementAsync(JsonElement element)
        {
            try
            {
                // itemTypeを取得
                if (!element.TryGetProperty("itemType", out var itemTypeElement))
                    return null;

                var itemType = itemTypeElement.GetString();
                if (string.IsNullOrEmpty(itemType))
                    return null;

                // DirectCommandRegistryを使用してアイテムを作成
                var item = AutoToolCommandRegistry.CreateUniversalItem(itemType);

                if (item == null)
                {
                    // フォールバック: 手動で作成
                    item = new UniversalCommandItem
                    {
                        ItemType = itemType
                    };
                }

                // 基本プロパティを設定（null値チェック追加）
                if (element.TryGetProperty("lineNumber", out var lineNumberElement))
                {
                    if (lineNumberElement.ValueKind == JsonValueKind.Number)
                    {
                        item.LineNumber = lineNumberElement.GetInt32();
                    }
                    else if (lineNumberElement.ValueKind == JsonValueKind.Null)
                    {
                        // nullの場合は自動採番
                        item.LineNumber = GetAutoLineNumber();
                        _logger.LogDebug("lineNumberがnullのため自動採番: {LineNumber}", item.LineNumber);
                    }
                }
                else
                {
                    // プロパティが存在しない場合も自動採番
                    item.LineNumber = GetAutoLineNumber();
                }

                if (element.TryGetProperty("isEnable", out var isEnableElement))
                {
                    if (isEnableElement.ValueKind == JsonValueKind.True || isEnableElement.ValueKind == JsonValueKind.False)
                    {
                        item.IsEnable = isEnableElement.GetBoolean();
                    }
                    // nullや無効な値の場合はデフォルト値(true)のまま
                }

                if (element.TryGetProperty("comment", out var commentElement))
                {
                    item.Comment = commentElement.ValueKind == JsonValueKind.String ? 
                                   (commentElement.GetString() ?? string.Empty) : string.Empty;
                }

                if (element.TryGetProperty("description", out var descriptionElement))
                {
                    item.Description = descriptionElement.ValueKind == JsonValueKind.String ? 
                                       (descriptionElement.GetString() ?? string.Empty) : string.Empty;
                }

                if (element.TryGetProperty("nestLevel", out var nestLevelElement))
                {
                    if (nestLevelElement.ValueKind == JsonValueKind.Number)
                    {
                        item.NestLevel = nestLevelElement.GetInt32();
                    }
                    // nullや無効な値の場合はデフォルト値(0)のまま
                }

                if (element.TryGetProperty("isInLoop", out var isInLoopElement))
                {
                    if (isInLoopElement.ValueKind == JsonValueKind.True || isInLoopElement.ValueKind == JsonValueKind.False)
                    {
                        item.IsInLoop = isInLoopElement.GetBoolean();
                    }
                    // nullや無効な値の場合はデフォルト値(false)のまま
                }

                if (element.TryGetProperty("isInIf", out var isInIfElement))
                {
                    if (isInIfElement.ValueKind == JsonValueKind.True || isInIfElement.ValueKind == JsonValueKind.False)
                    {
                        item.IsInIf = isInIfElement.GetBoolean();
                    }
                    // nullや無効な値の場合はデフォルト値(false)のまま
                }

                // 設定値を復元
                if (element.TryGetProperty("settings", out var settingsElement) && 
                    settingsElement.ValueKind == JsonValueKind.Object)
                {
                    await RestoreSettingsAsync(item, settingsElement);
                }

                // Pair情報を保存（後で関係を復元） - null値チェック追加
                if (element.TryGetProperty("pairLineNumber", out var pairElement) && 
                    pairElement.ValueKind == JsonValueKind.Number)
                {
                    var pairLineNumber = pairElement.GetInt32();
                    item.SetSetting("_PairLineNumber", pairLineNumber);
                }

                _logger.LogDebug("コマンド復元成功: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "コマンドエレメントの解析に失敗");
                return null;
            }
        }

        /// <summary>
        /// レガシー形式のコマンドエレメントを解析
        /// </summary>
        private async Task<UniversalCommandItem?> ParseLegacyCommandElementAsync(JsonElement element)
        {
            try
            {
                string? itemType = null;

                // 旧形式のtype/itemTypeを検索
                if (element.TryGetProperty("type", out var typeElement))
                    itemType = typeElement.GetString();
                else if (element.TryGetProperty("itemType", out var itemTypeElement))
                    itemType = itemTypeElement.GetString();

                if (string.IsNullOrEmpty(itemType))
                    return null;

                var item = AutoToolCommandRegistry.CreateUniversalItem(itemType) ?? new UniversalCommandItem
                {
                    ItemType = itemType
                };

                // レガシー形式のプロパティを変換（null値チェック追加）
                if (element.TryGetProperty("line", out var lineElement))
                {
                    if (lineElement.ValueKind == JsonValueKind.Number)
                        item.LineNumber = lineElement.GetInt32();
                    else
                        item.LineNumber = GetAutoLineNumber();
                }
                else if (element.TryGetProperty("lineNumber", out var lineNumberElement))
                {
                    if (lineNumberElement.ValueKind == JsonValueKind.Number)
                        item.LineNumber = lineNumberElement.GetInt32();
                    else
                        item.LineNumber = GetAutoLineNumber();
                }
                else
                {
                    item.LineNumber = GetAutoLineNumber();
                }

                if (element.TryGetProperty("enabled", out var enabledElement))
                {
                    if (enabledElement.ValueKind == JsonValueKind.True || enabledElement.ValueKind == JsonValueKind.False)
                        item.IsEnable = enabledElement.GetBoolean();
                }
                else if (element.TryGetProperty("isEnable", out var isEnableElement))
                {
                    if (isEnableElement.ValueKind == JsonValueKind.True || isEnableElement.ValueKind == JsonValueKind.False)
                        item.IsEnable = isEnableElement.GetBoolean();
                }

                // プロパティ/設定値を復元
                if (element.TryGetProperty("properties", out var propertiesElement))
                {
                    await RestoreSettingsAsync(item, propertiesElement);
                }
                else if (element.TryGetProperty("settings", out var settingsElement))
                {
                    await RestoreSettingsAsync(item, settingsElement);
                }

                _logger.LogDebug("レガシーコマンド復元成功: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "レガシーコマンドエレメントの解析に失敗");
                return null;
            }
        }

        /// <summary>
        /// 設定値を復元
        /// </summary>
        private async Task RestoreSettingsAsync(UniversalCommandItem item, JsonElement settingsElement)
        {
            try
            {
                // DirectCommandRegistryから設定定義を取得
                var settingDefinitions = AutoToolCommandRegistry.GetSettingDefinitions(item.ItemType);
                var definitionDict = settingDefinitions.ToDictionary(d => d.PropertyName, d => d);

                foreach (var property in settingsElement.EnumerateObject())
                {
                    var key = property.Name;
                    var valueElement = property.Value;

                    try
                    {
                        object? convertedValue = null;

                        // 設定定義がある場合は型に応じて変換
                        if (definitionDict.TryGetValue(key, out var definition))
                        {
                            convertedValue = ConvertJsonValueToSettingType(valueElement, definition.PropertyType);
                        }
                        else
                        {
                            // 設定定義がない場合は汎用的に変換
                            convertedValue = ConvertJsonValueGeneric(valueElement);
                        }

                        if (convertedValue != null)
                        {
                            item.SetSetting(key, convertedValue);
                            _logger.LogTrace("設定値復元: {ItemType}.{Key} = {Value}", 
                                item.ItemType, key, convertedValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "設定値の復元に失敗: {ItemType}.{Key} = {Value}",
                            item.ItemType, key, property.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定値復元中にエラー: {ItemType}", item.ItemType);
            }
        }

        /// <summary>
        /// JSON値を指定された型に変換
        /// </summary>
        private object? ConvertJsonValueToSettingType(JsonElement element, Type targetType)
        {
            try
            {
                if (element.ValueKind == JsonValueKind.Null)
                    return null;

                // Nullable型の処理
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType)!;
                }

                return targetType.Name switch
                {
                    nameof(String) => element.GetString(),
                    nameof(Int32) => element.GetInt32(),
                    nameof(Double) => element.GetDouble(),
                    nameof(Boolean) => element.GetBoolean(),
                    nameof(DateTime) => element.GetDateTime(),
                    nameof(Decimal) => element.GetDecimal(),
                    nameof(Single) => (float)element.GetDouble(),
                    nameof(Int64) => element.GetInt64(),
                    nameof(Int16) => (short)element.GetInt32(),
                    nameof(Byte) => (byte)element.GetInt32(),
                    _ => HandleComplexType(element, targetType)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JSON値の型変換に失敗: {Value} -> {TargetType}", element, targetType);
                return null;
            }
        }

        /// <summary>
        /// 複雑な型の処理
        /// </summary>
        private object? HandleComplexType(JsonElement element, Type targetType)
        {
            try
            {
                if (targetType.IsEnum)
                {
                    var stringValue = element.GetString();
                    if (!string.IsNullOrEmpty(stringValue) && Enum.TryParse(targetType, stringValue, true, out var enumValue))
                        return enumValue;
                }

                // Colorの特別処理
                if (targetType.Name == "Color" && element.ValueKind == JsonValueKind.String)
                {
                    var colorString = element.GetString();
                    if (!string.IsNullOrEmpty(colorString))
                    {
                        // System.Windows.Media.Colorの場合
                        var colorConverter = System.Windows.Media.ColorConverter.ConvertFromString(colorString);
                        return colorConverter;
                      }
                }

                // JSONデシリアライズを試行
                return element.Deserialize(targetType, DefaultSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "複雑な型の変換に失敗: {Type}", targetType);
                return null;
            }
        }

        /// <summary>
        /// 汎用的なJSON値変換
        /// </summary>
        private object? ConvertJsonValueGeneric(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        /// <summary>
        /// Pairの関係を復元
        /// </summary>
        private void RestorePairRelationships(List<UniversalCommandItem> items)
        {
            try
            {
                var itemsByLineNumber = items.ToDictionary(i => i.LineNumber, i => i);

                foreach (var item in items)
                {
                    var pairLineNumber = item.GetSetting<int?>("_PairLineNumber");
                    if (pairLineNumber.HasValue && itemsByLineNumber.TryGetValue(pairLineNumber.Value, out var pairItem))
                    {
                        item.Pair = pairItem;
                        pairItem.Pair = item;
                        _logger.LogDebug("Pair関係復元: {ItemType}(行{LineNumber}) <-> {PairType}(行{PairLineNumber})",
                            item.ItemType, item.LineNumber, pairItem.ItemType, pairItem.LineNumber);
                    }

                    // 一時的な設定値を削除
                    item.Settings.Remove("_PairLineNumber");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pair関係復元中にエラー");
            }
        }

        /// <summary>
        /// マクロファイルデータを作成
        /// </summary>
        private async Task<MacroFileData> CreateMacroFileDataAsync(string filePath, List<UniversalCommandItem> items, MacroFileMetadata? metadata)
        {
            var macroData = new MacroFileData
            {
                Version = metadata?.Version ?? "1.0",
                Name = metadata?.Name ?? Path.GetFileNameWithoutExtension(filePath),
                Description = metadata?.Description ?? $"AutoTool マクロファイル ({items.Count}個のコマンド)",
                Author = metadata?.Author ?? Environment.UserName,
                CreatedAt = metadata?.CreatedAt ?? DateTime.Now,
                ModifiedAt = DateTime.Now,
                Tags = metadata?.Tags ?? new List<string>(),
                Commands = await CreateCommandDataListAsync(items)
            };

            return macroData;
        }

        /// <summary>
        /// コマンドデータリストを作成
        /// </summary>
        private async Task<List<CommandItemData>> CreateCommandDataListAsync(List<UniversalCommandItem> items)
        {
            var commandList = new List<CommandItemData>();

            foreach (var item in items)
            {
                try
                {
                    var commandData = new CommandItemData
                    {
                        ItemType = item.ItemType,
                        LineNumber = item.LineNumber,
                        IsEnable = item.IsEnable,
                        Comment = item.Comment ?? string.Empty,
                        Description = item.Description ?? string.Empty,
                        NestLevel = item.NestLevel,
                        IsInLoop = item.IsInLoop,
                        IsInIf = item.IsInIf,
                        Settings = SerializeSettings(item),
                        PairLineNumber = item.Pair?.LineNumber
                    };

                    commandList.Add(commandData);
                    _logger.LogDebug("コマンドシリアライズ成功: {ItemType} (行 {LineNumber})",
                        item.ItemType, item.LineNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "コマンドシリアライズ失敗: {ItemType} (行 {LineNumber})",
                        item.ItemType, item.LineNumber);
                }
            }

            return commandList;
        }

        /// <summary>
        /// 設定値をシリアライズ
        /// </summary>
        private Dictionary<string, object> SerializeSettings(UniversalCommandItem item)
        {
            var serializedSettings = new Dictionary<string, object>();

            try
            {
                // DirectCommandRegistryから設定定義を取得
                var settingDefinitions = AutoToolCommandRegistry.GetSettingDefinitions(item.ItemType);

                foreach (var definition in settingDefinitions)
                {
                    var value = item.GetSetting<object>(definition.PropertyName);
                    if (value != null)
                    {
                        var serializedValue = SerializeSettingValue(value, definition.PropertyType);
                        if (serializedValue != null)
                        {
                            serializedSettings[definition.PropertyName] = serializedValue;
                        }
                    }
                }

                // 定義にない設定値も保存（拡張性のため）
                foreach (var kvp in item.Settings)
                {
                    if (!serializedSettings.ContainsKey(kvp.Key) && kvp.Value != null)
                    {
                        var serializedValue = SerializeSettingValue(kvp.Value, kvp.Value.GetType());
                        if (serializedValue != null)
                        {
                            serializedSettings[kvp.Key] = serializedValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定値シリアライズ中にエラー: {ItemType}", item.ItemType);
            }

            return serializedSettings;
        }

        /// <summary>
        /// 設定値をシリアライズ
        /// </summary>
        private object? SerializeSettingValue(object value, Type valueType)
        {
            try
            {
                // プリミティブ型はそのまま
                if (valueType.IsPrimitive || value is string or DateTime or decimal)
                {
                    return value;
                }

                // Enumは文字列に変換
                if (valueType.IsEnum)
                {
                    return value.ToString();
                }

                // Colorの特別処理
                if (value is System.Windows.Media.Color color)
                {
                    return color.ToString();
                }

                // その他はJSON文字列に変換
                return JsonSerializer.Serialize(value, DefaultSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "設定値のシリアライズに失敗: {Value} ({Type})", value, valueType);
                return value.ToString();
            }
        }

        #endregion
    }
}