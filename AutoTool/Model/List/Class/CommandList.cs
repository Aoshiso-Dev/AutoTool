using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Class;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.List.Class;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTool.Model.MacroFactory;
using AutoTool.Services.Configuration;
using AutoTool.Helpers;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AutoTool.List.Class
{
    /// <summary>
    /// コマンドリストの管理サービス（ILogger対応版）
    /// UIバインディングは不要なため、ObservableObjectは継承しない
    /// </summary>
    public class CommandListService
    {
        private readonly ILogger<CommandListService> _logger;
        private readonly ObservableCollection<ICommandListItem> _items = new();

        /// <summary>
        /// アイテムコレクション（読み取り専用プロパティ）
        /// </summary>
        public ObservableCollection<ICommandListItem> Items => _items;

        /// <summary>
        /// インデクサ
        /// </summary>
        public ICommandListItem this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        /// <summary>
        /// コンストラクタ（ILogger注入）
        /// </summary>
        public CommandListService(ILogger<CommandListService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("CommandListService初期化完了");
        }

        /// <summary>
        /// リスト変更後の共通処理
        /// </summary>
        private void RefreshListState()
        {
            try
            {
                _logger.LogTrace("リスト状態更新開始");
                ReorderItems();
                CalculateNestLevel();
                PairIfItems();
                PairLoopItems();
                _logger.LogTrace("リスト状態更新完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "リスト状態更新中にエラーが発生しました");
                throw;
            }
        }

        #region コレクション操作メソッド

        public void Add(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                _items.Add(item);
                RefreshListState();
                _logger.LogDebug("アイテム追加: {ItemType} (総数: {Count})", item.ItemType, _items.Count);
            });
        }

        public void Remove(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                _items.Remove(item);
                RefreshListState();
                _logger.LogDebug("アイテム削除: {ItemType} (総数: {Count})", item.ItemType, _items.Count);
            });
        }

        public void RemoveAt(int index)
        {
            ExecuteOnUIThread(() =>
            {
                if (index >= 0 && index < _items.Count)
                {
                    var item = _items[index];
                    _items.RemoveAt(index);
                    RefreshListState();
                    _logger.LogDebug("インデックス {Index} のアイテム削除: {ItemType} (総数: {Count})", 
                        index, item.ItemType, _items.Count);
                }
                else
                {
                    _logger.LogWarning("無効なインデックスでの削除試行: {Index} (有効範囲: 0-{MaxIndex})", 
                        index, _items.Count - 1);
                }
            });
        }

        public void Insert(int index, ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                _items.Insert(index, item);
                RefreshListState();
                _logger.LogDebug("アイテム挿入: {ItemType} at {Index} (総数: {Count})", 
                    item.ItemType, index, _items.Count);
            });
        }

        public void Override(int index, ICommandListItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            ExecuteOnUIThread(() =>
            {
                if (index < 0 || index >= _items.Count)
                {
                    _logger.LogError("インデックス範囲外: {Index} (有効範囲: 0-{MaxIndex})", 
                        index, _items.Count - 1);
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                var oldItem = _items[index];
                _items[index] = item;
                RefreshListState();
                _logger.LogDebug("アイテム置換: {OldType} -> {NewType} at {Index}", 
                    oldItem.ItemType, item.ItemType, index);
            });
        }

        public void Clear()
        {
            ExecuteOnUIThread(() =>
            {
                var count = _items.Count;
                _items.Clear();
                _logger.LogInformation("全アイテムクリア: {Count}件削除", count);
            });
        }

        public void Move(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= _items.Count || newIndex < 0 || newIndex >= _items.Count)
                {
                    _logger.LogWarning("無効なインデックスでの移動試行: {OldIndex} -> {NewIndex} (有効範囲: 0-{MaxIndex})", 
                        oldIndex, newIndex, _items.Count - 1);
                    return;
                }

                var item = _items[oldIndex];
                _items.RemoveAt(oldIndex);
                _items.Insert(newIndex, item);
                RefreshListState();
                _logger.LogDebug("アイテム移動: {ItemType} from {OldIndex} to {NewIndex}", 
                    item.ItemType, oldIndex, newIndex);
            });
        }

        public void Copy(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= _items.Count || newIndex < 0 || newIndex >= _items.Count)
                {
                    _logger.LogWarning("無効なインデックスでのコピー試行: {OldIndex} -> {NewIndex} (有効範囲: 0-{MaxIndex})", 
                        oldIndex, newIndex, _items.Count - 1);
                    return;
                }

                var item = _items[oldIndex];
                _items.Insert(newIndex, item);
                RefreshListState();
                _logger.LogDebug("アイテムコピー: {ItemType} from {OldIndex} to {NewIndex}", 
                    item.ItemType, oldIndex, newIndex);
            });
        }

        #endregion

        #region リスト整理メソッド

        public void ReorderItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].LineNumber = i + 1;
            }
            _logger.LogTrace("行番号再配列完了: {Count}件", _items.Count);
        }

        public void CalculateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in _items)
            {
                // ネストレベルを減らすコマンド（終了系）
                if (CommandRegistry.IsEndCommand(item.ItemType))
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                // ネストレベルを増やすコマンド（開始系）
                if (CommandRegistry.IsStartCommand(item.ItemType))
                {
                    nestLevel++;
                }
            }
            _logger.LogTrace("ネストレベル計算完了: 最大レベル {MaxLevel}", nestLevel);
        }

        private void PairItems<TStart, TEnd>(Func<ICommandListItem, bool> startPredicate, Func<ICommandListItem, bool> endPredicate)
            where TStart : class
            where TEnd : class
        {
            var startItems = _items.OfType<TStart>().Cast<ICommandListItem>()
                .Where(startPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            var endItems = _items.OfType<TEnd>().Cast<ICommandListItem>()
                .Where(endPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            var pairedCount = 0;
            foreach (var startItem in startItems)
            {
                var startPairItem = startItem as dynamic;
                if (startPairItem?.Pair != null) continue;

                foreach (var endItem in endItems)
                {
                    var endPairItem = endItem as dynamic;
                    if (endPairItem?.Pair != null) continue;

                    if (endItem.NestLevel == startItem.NestLevel && endItem.LineNumber > startItem.LineNumber)
                    {
                        startPairItem.Pair = endItem;
                        endPairItem.Pair = startItem;
                        pairedCount++;
                        break;
                    }
                }
            }
            _logger.LogTrace("ペアリング完了: {StartType}-{EndType} {PairedCount}組", 
                typeof(TStart).Name, typeof(TEnd).Name, pairedCount);
        }

        public void PairIfItems()
        {
            try
            {
                PairItems<AutoTool.Model.List.Interface.IIfItem, AutoTool.Model.List.Interface.IIfEndItem>(
                    x => CommandRegistry.IsIfCommand(x.ItemType),
                    x => x.ItemType == CommandRegistry.CommandTypes.IfEnd
                );
                _logger.LogTrace("Ifアイテムペアリング完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ifアイテムペアリング中にエラーが発生しました");
            }
        }

        public void PairLoopItems()
        {
            try
            {
                PairItems<AutoTool.Model.List.Interface.ILoopItem, AutoTool.Model.List.Interface.ILoopEndItem>(
                    x => CommandRegistry.IsLoopCommand(x.ItemType),
                    x => x.ItemType == CommandRegistry.CommandTypes.LoopEnd
                );
                _logger.LogTrace("Loopアイテムペアリング完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loopアイテムペアリング中にエラーが発生しました");
            }
        }

        #endregion

        #region ファイル操作メソッド

        public IEnumerable<ICommandListItem> Clone()
        {
            var clone = new List<ICommandListItem>();

            foreach (var item in _items)
            {
                clone.Add(item.Clone());
            }

            _logger.LogDebug("リストクローン作成: {Count}件", clone.Count);
            return clone;
        }

        public void Save(string filePath)
        {
            try
            {
                _logger.LogInformation("ファイル保存開始: {FilePath}", filePath);
                var cloneItems = Clone().ToList();
                JsonSerializerHelper.SerializeToFile(cloneItems, filePath);
                _logger.LogInformation("ファイル保存完了: {FilePath} ({Count}件)", filePath, cloneItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存に失敗しました: {FilePath}", filePath);
                throw new InvalidOperationException($"ファイル保存に失敗しました: {ex.Message}", ex);
            }
        }

        public void Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogError("ファイルが見つかりません: {FilePath}", filePath);
                    throw new FileNotFoundException($"ファイルが見つかりません: {filePath}");
                }

                _logger.LogInformation("ファイル読み込み開始: {FilePath}", filePath);

                var jsonContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("空のファイルです: {FilePath}", filePath);
                    ExecuteOnUIThread(() => _items.Clear());
                    return;
                }

                _logger.LogDebug("JSON内容サイズ: {Size}文字", jsonContent.Length);

                List<ICommandListItem>? deserializedItems = null;

                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object && 
                        root.TryGetProperty("$values", out var valuesElement) &&
                        valuesElement.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogDebug("参照保持形式として処理");
                        deserializedItems = ProcessReferencePreservationFormat(valuesElement);
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogDebug("通常配列形式として処理");
                        deserializedItems = ProcessNormalArrayFormat(root);
                    }
                    else
                    {
                        _logger.LogDebug("標準デシリアライゼーションを試行");
                        deserializedItems = JsonSerializerHelper.DeserializeFromFile<List<ICommandListItem>>(filePath);
                    }
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "JSON解析エラー");
                    
                    try
                    {
                        deserializedItems = JsonSerializerHelper.DeserializeFromFile<List<ICommandListItem>>(filePath);
                        _logger.LogInformation("フォールバックで処理完了: {Count}件", deserializedItems?.Count ?? 0);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "フォールバック失敗");
                        throw new InvalidDataException($"JSONファイルの解析に失敗しました: {parseEx.Message}", parseEx);
                    }
                }

                if (deserializedItems != null && deserializedItems.Count > 0)
                {
                    _logger.LogDebug("UIスレッドでアイテム追加開始: {Count}個", deserializedItems.Count);
                    
                    ExecuteOnUIThread(() =>
                    {
                        try
                        {
                            _items.Clear();

                            foreach (var item in deserializedItems)
                            {
                                if (item != null)
                                {
                                    ValidateAndRepairItem(item);
                                    _items.Add(item);
                                    _logger.LogTrace("アイテム追加: {ItemType} - {Comment}", item.ItemType, item.Comment);
                                }
                            }

                            RefreshListState();
                            _logger.LogInformation("ファイル読み込み完了: {FilePath} ({Count}個のアイテム)", filePath, _items.Count);
                        }
                        catch (Exception uiEx)
                        {
                            _logger.LogError(uiEx, "UIスレッド処理中にエラー");
                            throw;
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("有効なアイテムが見つかりませんでした: {FilePath}", filePath);
                    ExecuteOnUIThread(() => _items.Clear());
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSONファイル形式エラー: {FilePath}", filePath);
                throw new InvalidDataException($"JSONファイルの形式が無効です: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込みエラー: {FilePath}", filePath);
                throw new InvalidOperationException($"ファイル読み込みに失敗しました: {ex.Message}", ex);
            }
        }

        #endregion

        #region JSONデシリアライゼーション

        private List<ICommandListItem> ProcessReferencePreservationFormat(JsonElement valuesElement)
        {
            try
            {
                _logger.LogDebug("参照保持形式処理開始");
                
                var items = new List<ICommandListItem>();
                var referenceMap = new Dictionary<string, ICommandListItem>();

                var arrayLength = valuesElement.GetArrayLength();
                _logger.LogDebug("配列要素数: {Count}", arrayLength);

                // 第1パス: アイテム作成
                int elementIndex = 0;
                foreach (var element in valuesElement.EnumerateArray())
                {
                    try
                    {
                        var item = CreateCommandItemFromElement(element);
                        if (item != null)
                        {
                            items.Add(item);
                            _logger.LogTrace("アイテム作成成功[{Index}]: {ItemType}", elementIndex, item.ItemType);
                            
                            if (element.TryGetProperty("$id", out var idElement))
                            {
                                var id = idElement.GetString();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    referenceMap[id] = item;
                                }
                            }
                        }
                        elementIndex++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "要素[{Index}]処理中にエラー", elementIndex);
                        elementIndex++;
                    }
                }

                // 第2パス: 参照解決
                for (int i = 0; i < items.Count; i++)
                {
                    try
                    {
                        var element = valuesElement.EnumerateArray().ElementAt(i);
                        ResolvePairReferences(items[i], element, referenceMap);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "参照解決[{Index}]でエラー", i);
                    }
                }

                _logger.LogDebug("参照保持形式処理完了: {Count}件", items.Count);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "参照保持形式処理で全体エラー");
                throw;
            }
        }

        private List<ICommandListItem> ProcessNormalArrayFormat(JsonElement arrayElement)
        {
            var items = new List<ICommandListItem>();

            foreach (var element in arrayElement.EnumerateArray())
            {
                var item = CreateCommandItemFromElement(element);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            _logger.LogDebug("通常配列形式処理完了: {Count}件", items.Count);
            return items;
        }

        private ICommandListItem? CreateCommandItemFromElement(JsonElement element)
        {
            try
            {
                string? itemType = GetPropertyValue<string>(element, "ItemType", "itemType");
                
                if (string.IsNullOrEmpty(itemType))
                {
                    itemType = InferItemTypeFromProperties(element);
                    _logger.LogTrace("ItemType推定: {ItemType}", itemType);
                }
                else
                {
                    _logger.LogTrace("ItemType取得: {ItemType}", itemType);
                }

                ICommandListItem? item = null;
                if (!string.IsNullOrEmpty(itemType))
                {
                    try
                    {
                        item = CommandRegistry.CreateCommandItem(itemType);
                        if (item != null)
                        {
                            _logger.LogTrace("CommandRegistry作成成功: {ActualType}", item.GetType().Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CommandRegistry作成エラー for {ItemType}", itemType);
                    }
                }

                if (item == null)
                {
                    item = new BasicCommandItem();
                    item.ItemType = itemType ?? "Unknown";
                    _logger.LogDebug("BasicCommandItemフォールバック: {ItemType}", item.ItemType);
                }

                RestorePropertiesFromElement(item, element);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム作成中にエラー");
                return null;
            }
        }

        #endregion

        #region ヘルパーメソッド

        private static string InferItemTypeFromProperties(JsonElement element)
        {
            if (HasProperty(element, "LoopCount", "loopCount")) return "Loop";
            
            if (HasProperty(element, "ImagePath", "imagePath"))
            {
                if (HasProperty(element, "ModelPath", "modelPath")) return "Click_Image_AI";
                if (HasProperty(element, "Timeout", "timeout")) return "Wait_Image";
                return "Click_Image";
            }
            
            if (HasProperty(element, "Wait", "wait")) return "Wait";
            if (HasProperty(element, "X", "x") && HasProperty(element, "Y", "y")) return "Click";
            
            if (HasProperty(element, "Key", "key") && 
                (HasProperty(element, "Ctrl", "ctrl") || HasProperty(element, "Alt", "alt") || HasProperty(element, "Shift", "shift")))
                return "Hotkey";
            
            if (HasProperty(element, "Pair", "pair"))
            {
                var desc = GetPropertyValue<string>(element, "Description", "description") ?? "";
                if (desc.Contains("->") || desc.Contains("End"))
                {
                    return desc.Contains("Loop") || desc.Contains("ループ") ? "Loop_End" : "IF_End";
                }
            }

            return "Unknown";
        }

        private static void RestorePropertiesFromElement(ICommandListItem item, JsonElement element)
        {
            item.Comment = GetPropertyValue<string>(element, "Comment", "comment") ?? string.Empty;
            item.Description = GetPropertyValue<string>(element, "Description", "description") ?? string.Empty;
            item.IsEnable = GetPropertyValue<bool>(element, "IsEnable", "isEnable");
            item.LineNumber = GetPropertyValue<int>(element, "LineNumber", "lineNumber");
            item.NestLevel = GetPropertyValue<int>(element, "NestLevel", "nestLevel");
            item.IsInLoop = GetPropertyValue<bool>(element, "IsInLoop", "isInLoop");
            item.IsInIf = GetPropertyValue<bool>(element, "IsInIf", "isInIf");
            item.Progress = GetPropertyValue<int>(element, "Progress", "progress");

            RestoreTypeSpecificPropertiesFromElement(item, element);
        }

        private static void RestoreTypeSpecificPropertiesFromElement(ICommandListItem item, JsonElement element)
        {
            var itemType = item.GetType();

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.StartsWith("$")) continue;

                var propInfo = FindProperty(itemType, property.Name);
                if (propInfo != null && propInfo.CanWrite)
                {
                    try
                    {
                        var value = ConvertJsonValueToPropertyType(property.Value, propInfo.PropertyType);
                        if (value != null)
                        {
                            propInfo.SetValue(item, value);
                        }
                    }
                    catch
                    {
                        // プロパティ設定失敗は無視
                    }
                }
            }
        }

        private static void ResolvePairReferences(ICommandListItem item, JsonElement element, Dictionary<string, ICommandListItem> referenceMap)
        {
            if (element.TryGetProperty("Pair", out var pairElement) && 
                pairElement.ValueKind == JsonValueKind.Object &&
                pairElement.TryGetProperty("$ref", out var refElement))
            {
                var refId = refElement.GetString();
                if (!string.IsNullOrEmpty(refId) && referenceMap.TryGetValue(refId, out var pairItem))
                {
                    var pairProperty = item.GetType().GetProperty("Pair");
                    if (pairProperty != null && pairProperty.CanWrite)
                    {
                        pairProperty.SetValue(item, pairItem);
                    }
                }
            }
        }

        private static void ValidateAndRepairItem(ICommandListItem item)
        {
            if (string.IsNullOrEmpty(item.ItemType))
            {
                item.ItemType = item.GetType().Name.Replace("Item", "");
            }
            
            if (string.IsNullOrEmpty(item.Description))
            {
                item.Description = $"{item.ItemType}コマンド";
            }
        }

        private static T GetPropertyValue<T>(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var propElement))
                {
                    return ConvertJsonValueToType<T>(propElement);
                }
            }
            return default(T)!;
        }

        private static bool HasProperty(JsonElement element, params string[] propertyNames)
        {
            return propertyNames.Any(name => element.TryGetProperty(name, out _));
        }

        private static T ConvertJsonValueToType<T>(JsonElement element)
        {
            try
            {
                var targetType = typeof(T);
                return (T)ConvertJsonValueToPropertyType(element, targetType)!;
            }
            catch
            {
                return default(T)!;
            }
        }

        private static object? ConvertJsonValueToPropertyType(JsonElement element, Type targetType)
        {
            try
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (element.ValueKind == JsonValueKind.Null) return null;
                if (underlyingType == typeof(string)) return element.GetString();
                
                if (underlyingType == typeof(int))
                    return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : 
                           element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var i) ? i : 0;
                
                if (underlyingType == typeof(double))
                    return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : 
                           element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var d) ? d : 0.0;
                
                if (underlyingType == typeof(bool))
                    return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False ? element.GetBoolean() :
                           element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var b) ? b : false;

                if (underlyingType.IsEnum)
                {
                    var stringValue = element.GetString();
                    return !string.IsNullOrEmpty(stringValue) && Enum.TryParse(underlyingType, stringValue, true, out var enumValue) ? 
                           enumValue : Enum.GetValues(underlyingType).GetValue(0);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static PropertyInfo? FindProperty(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) ??
                   type.GetProperty(char.ToUpper(propertyName[0]) + propertyName.Substring(1));
        }

        private void ExecuteOnUIThread(Action action)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    if (app.Dispatcher.CheckAccess())
                    {
                        action();
                    }
                    else
                    {
                        app.Dispatcher.Invoke(action);
                    }
                }
                else
                {
                    _logger.LogWarning("Application.Currentがnullのため、直接実行します");
                    action();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UIスレッド実行中にエラー");
                throw new InvalidOperationException($"UIスレッドでの実行に失敗しました: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
