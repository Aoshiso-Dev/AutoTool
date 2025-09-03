using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Class;
using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Reflection; // リフレクション用

namespace AutoTool.List.Class
{
    public partial class CommandList : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        public ICommandListItem this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        /// <summary>
        /// リスト変更後の共通処理
        /// </summary>
        private void RefreshListState()
        {
            ReorderItems();
            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }

        public void Add(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                Items.Add(item);
                RefreshListState();
            });
        }

        public void Remove(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                Items.Remove(item);
                RefreshListState();
            });
        }

        /// <summary>
        /// 指定インデックスのアイテムを削除
        /// </summary>
        public void RemoveAt(int index)
        {
            ExecuteOnUIThread(() =>
            {
                if (index >= 0 && index < Items.Count)
                {
                    Items.RemoveAt(index);
                    RefreshListState();
                }
            });
        }

        public void Insert(int index, ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                Items.Insert(index, item);
                RefreshListState();
            });
        }

        public void Override(int index, ICommandListItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            ExecuteOnUIThread(() =>
            {
                if (index < 0 || index >= Items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                Items[index] = item;
                RefreshListState();
            });
        }

        public void Clear()
        {
            ExecuteOnUIThread(() =>
            {
                Items.Clear();
            });
        }

        public void Move(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
                    return;

                var item = Items[oldIndex];
                Items.RemoveAt(oldIndex);
                Items.Insert(newIndex, item);
                RefreshListState();
            });
        }

        public void Copy(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
                    return;

                var item = Items[oldIndex];
                Items.Insert(newIndex, item);
                RefreshListState();
            });
        }

        public void ReorderItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
        }

        public void CalculateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in Items)
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
        }

        /// <summary>
        /// 共通のペアリング処理
        /// </summary>
        private void PairItems<TStart, TEnd>(Func<ICommandListItem, bool> startPredicate, Func<ICommandListItem, bool> endPredicate)
            where TStart : class
            where TEnd : class
        {
            var startItems = Items.OfType<TStart>().Cast<ICommandListItem>()
                .Where(startPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            var endItems = Items.OfType<TEnd>().Cast<ICommandListItem>()
                .Where(endPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

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
                        break;
                    }
                }
            }
        }

        public void PairIfItems()
        {
            PairItems<AutoTool.Model.List.Interface.IIfItem, AutoTool.Model.List.Interface.IIfEndItem>(
                x => CommandRegistry.IsIfCommand(x.ItemType),
                x => x.ItemType == CommandRegistry.CommandTypes.IfEnd
            );
        }

        public void PairLoopItems()
        {
            PairItems<AutoTool.Model.List.Interface.ILoopItem, AutoTool.Model.List.Interface.ILoopEndItem>(
                x => CommandRegistry.IsLoopCommand(x.ItemType),
                x => x.ItemType == CommandRegistry.CommandTypes.LoopEnd
            );
        }

        public IEnumerable<ICommandListItem> Clone()
        {
            var clone = new List<ICommandListItem>();

            foreach (var item in Items)
            {
                clone.Add(item.Clone());
            }

            return clone;
        }

        public void Save(string filePath)
        {
            try
            {
                var cloneItems = Clone().ToList();
                JsonSerializerHelper.SerializeToFile(cloneItems, filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ファイル保存に失敗しました: {ex.Message}", ex);
            }
        }

        public void Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"ファイルが見つかりません: {filePath}");
                }

                System.Diagnostics.Debug.WriteLine($"[CommandList] ファイル読み込み開始: {filePath}");

                // ファイル内容を確認
                var jsonContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    System.Diagnostics.Debug.WriteLine("[CommandList] 空のファイルです");
                    
                    // UIスレッドで実行（安全確認付き）
                    ExecuteOnUIThread(() =>
                    {
                        Items.Clear();
                    });
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[CommandList] JSON内容サイズ: {jsonContent.Length}文字");
                System.Diagnostics.Debug.WriteLine($"[CommandList] JSON内容の先頭200文字: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}");

                // JSONの基本構造を分析
                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;
                    System.Diagnostics.Debug.WriteLine($"[CommandList] JSONルート要素: ValueKind={root.ValueKind}");
                    
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        System.Diagnostics.Debug.WriteLine("[CommandList] JSONはオブジェクト形式");
                        if (root.TryGetProperty("$id", out var idProp))
                        {
                            System.Diagnostics.Debug.WriteLine($"[CommandList] $id プロパティ: {idProp.GetString()}");
                        }
                        if (root.TryGetProperty("$values", out var valuesProp))
                        {
                            System.Diagnostics.Debug.WriteLine($"[CommandList] $values プロパティ: ValueKind={valuesProp.ValueKind}");
                            if (valuesProp.ValueKind == JsonValueKind.Array)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CommandList] $values 配列長: {valuesProp.GetArrayLength()}");
                            }
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CommandList] JSONは配列形式: 要素数={root.GetArrayLength()}");
                    }
                }
                catch (Exception parseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandList] JSON構造分析エラー: {parseEx.Message}");
                }

                List<ICommandListItem>? deserializedItems = null;

                try
                {
                    System.Diagnostics.Debug.WriteLine("[CommandList] === List<ICommandListItem>として読み込み試行 ===");
                    // List<ICommandListItem>として試行
                    deserializedItems = JsonSerializerHelper.DeserializeFromFile<List<ICommandListItem>>(filePath);
                    System.Diagnostics.Debug.WriteLine($"[CommandList] List<ICommandListItem>として読み込み成功: {deserializedItems?.Count ?? 0}個");
                    
                    if (deserializedItems != null)
                    {
                        for (int i = 0; i < Math.Min(5, deserializedItems.Count); i++)
                        {
                            var item = deserializedItems[i];
                            System.Diagnostics.Debug.WriteLine($"[CommandList] 読み込みアイテム[{i}]: Type={item?.GetType().Name}, ItemType={item?.ItemType}, Comment={item?.Comment}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandList] List<ICommandListItem>での読み込み失敗: {ex.GetType().Name} - {ex.Message}");
                    
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[CommandList] === ObservableCollection<ICommandListItem>として読み込み試行 ===");
                        // ObservableCollection<ICommandListItem>として試行
                        var obsCollection = JsonSerializerHelper.DeserializeFromFile<ObservableCollection<ICommandListItem>>(filePath);
                        deserializedItems = obsCollection?.ToList();
                        System.Diagnostics.Debug.WriteLine($"[CommandList] ObservableCollection<ICommandListItem>として読み込み成功: {deserializedItems?.Count ?? 0}個");
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CommandList] ObservableCollection<ICommandListItem>での読み込み失敗: {ex2.GetType().Name} - {ex2.Message}");
                        
                        // 最後の手段：JSONを手動解析してBasicCommandItemとして読み込み
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[CommandList] === 手動JSON解析として読み込み試行 ===");
                            using var doc = JsonDocument.Parse(jsonContent);
                            
                            // 参照保持形式の場合は$valuesを取得
                            JsonElement arrayElement;
                            if (doc.RootElement.ValueKind == JsonValueKind.Object && 
                                doc.RootElement.TryGetProperty("$values", out var valuesElement))
                            {
                                System.Diagnostics.Debug.WriteLine("[CommandList] 参照保持形式として手動解析");
                                arrayElement = valuesElement;
                            }
                            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                System.Diagnostics.Debug.WriteLine("[CommandList] 通常配列形式として手動解析");
                                arrayElement = doc.RootElement;
                            }
                            else
                            {
                                throw new InvalidDataException($"未対応のJSON形式: {doc.RootElement.ValueKind}");
                            }

                            if (arrayElement.ValueKind == JsonValueKind.Array)
                            {
                                deserializedItems = new List<ICommandListItem>();
                                var arrayLength = arrayElement.GetArrayLength();
                                System.Diagnostics.Debug.WriteLine($"[CommandList] 手動解析配列要素数: {arrayLength}");
                        
                                int elementIndex = 0;
                                foreach (var element in arrayElement.EnumerateArray())
                                {
                                    var basicItem = CreateBasicCommandItemFromJson(element);
                                    if (basicItem is not null)
                                    {
                                        deserializedItems.Add(basicItem);
                                        System.Diagnostics.Debug.WriteLine($"[CommandList] 手動解析要素[{elementIndex}]: {basicItem.ItemType} - {basicItem.Comment}");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[CommandList] 手動解析要素[{elementIndex}]: 作成失敗");
                                    }
                                    elementIndex++;
                                }
                                System.Diagnostics.Debug.WriteLine($"[CommandList] 手動解析で読み込み成功: {deserializedItems.Count}個");
                            }
                        }
                        catch (Exception ex3)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CommandList] 手動解析も失敗: {ex3.GetType().Name} - {ex3.Message}");
                            System.Diagnostics.Debug.WriteLine($"[CommandList] スタックトレース: {ex3.StackTrace}");
                            throw new InvalidDataException($"JSONファイルの解析に完全に失敗しました: {ex3.Message}", ex3);
                        }
                    }
                }

                if (deserializedItems != null && deserializedItems.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandList] === UIスレッドでアイテム追加開始: {deserializedItems.Count}個 ===");
                    
                    // UIスレッドでObservableCollectionを操作（安全確認付き）
                    ExecuteOnUIThread(() =>
                    {
                        Items.Clear();

                        foreach (var item in deserializedItems)
                        {
                            if (item != null)
                            {
                                // 基本プロパティの検証・修復
                                if (string.IsNullOrEmpty(item.ItemType))
                                {
                                    System.Diagnostics.Debug.WriteLine("[CommandList] ItemTypeが空のアイテムを修復");
                                    item.ItemType = item.GetType().Name.Replace("Item", "");
                                }

                                // Addメソッド内でRefreshListStateが呼ばれるため、直接Itemsに追加
                                Items.Add(item);
                                System.Diagnostics.Debug.WriteLine($"[CommandList] アイテム追加完了: {item.ItemType} - {item.Comment}");
                            }
                        }

                        // リスト状態を更新（UIスレッドで実行）
                        RefreshListState();
                        
                        System.Diagnostics.Debug.WriteLine($"[CommandList] === ファイル読み込み完了: {Items.Count}個のアイテム ===");
                    });
                }
                else
                {
                    // 空のファイルまたは無効なJSON
                    System.Diagnostics.Debug.WriteLine("[CommandList] 有効なアイテムが見つかりませんでした");
                    ExecuteOnUIThread(() =>
                    {
                        Items.Clear();
                    });
                }
            }
            catch (JsonException jsonEx)
            {
                var errorMessage = $"JSONファイルの形式が無効です: {jsonEx.Message}";
                System.Diagnostics.Debug.WriteLine($"[CommandList] {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[CommandList] JsonException詳細: {jsonEx}");
                throw new InvalidDataException(errorMessage, jsonEx);
            }
            catch (Exception ex)
            {
                var errorMessage = $"ファイル読み込みに失敗しました: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CommandList] {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[CommandList] Exception詳細: {ex}");
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        /// <summary>
        /// UIスレッドで安全に実行するヘルパーメソッド
        /// </summary>
        private static void ExecuteOnUIThread(Action action)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    if (app.Dispatcher.CheckAccess())
                    {
                        // 既にUIスレッドで実行中
                        action();
                    }
                    else
                    {
                        // 別スレッドからの呼び出し - Dispatcherを使用
                        app.Dispatcher.Invoke(action);
                    }
                }
                else
                {
                    // Applicationが存在しない場合（テスト環境など）
                    System.Diagnostics.Debug.WriteLine("Application.Currentがnullのため、直接実行します");
                    action();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UIスレッド実行中にエラー: {ex.Message}");
                throw new InvalidOperationException($"UIスレッドでの実行に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// JsonElementから適切な型のCommandListItemを作成
        /// </summary>
        private static ICommandListItem? CreateBasicCommandItemFromJson(JsonElement element)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] 要素処理開始: ValueKind={element.ValueKind}");

                // 要素の全プロパティを列挙してデバッグ
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] プロパティ: {property.Name} = {property.Value} (Type: {property.Value.ValueKind})");
                    }
                }

                // ItemType の取得（複数のプロパティ名を試行）
                string? itemType = null;
                if (element.TryGetProperty("itemType", out var itemTypeElement))
                {
                    itemType = itemTypeElement.GetString();
                }
                else if (element.TryGetProperty("ItemType", out var itemTypeElement2))
                {
                    itemType = itemTypeElement2.GetString();
                }

                if (!string.IsNullOrEmpty(itemType))
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] ItemType取得: {itemType}");
                }
                else
                {
                    // 他のプロパティから型を推定
                    if (element.TryGetProperty("loopCount", out _) || element.TryGetProperty("LoopCount", out _))
                    {
                        itemType = "Loop";
                    }
                    else if (element.TryGetProperty("imagePath", out _) || element.TryGetProperty("ImagePath", out _))
                    {
                        if (element.TryGetProperty("modelPath", out _) || element.TryGetProperty("ModelPath", out _))
                        {
                            itemType = "Click_Image_AI";
                        }
                        else
                        {
                            itemType = "Click_Image";
                        }
                    }
                    else if (element.TryGetProperty("wait", out _) || element.TryGetProperty("Wait", out _))
                    {
                        itemType = "Wait";
                    }
                    else if (element.TryGetProperty("x", out _) && element.TryGetProperty("y", out _))
                    {
                        itemType = "Click";
                    }
                    else if (element.TryGetProperty("key", out _) || element.TryGetProperty("Key", out _))
                    {
                        itemType = "Hotkey";
                    }
                    else if (element.TryGetProperty("pair", out _) || element.TryGetProperty("Pair", out _))
                    {
                        // PairがあるがLoopCountがない場合はIF系かLoop_End系
                        if (element.TryGetProperty("description", out var desc) && 
                            desc.ValueKind == JsonValueKind.String)
                        {
                            var descText = desc.GetString() ?? "";
                            if (descText.Contains("->") || descText.Contains("End"))
                            {
                                itemType = "IF_End"; // もしくは Loop_End
                            }
                        }
                    }
                    else
                    {
                        itemType = "Unknown";
                    }
                    System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] ItemType推定: {itemType}");
                }

                // CommandRegistryを使用して適切な型のアイテムを作成
                ICommandListItem? item = null;
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (!string.IsNullOrEmpty(itemType) && itemTypes.TryGetValue(itemType, out var targetType))
                {
                    try
                    {
                        item = (ICommandListItem?)Activator.CreateInstance(targetType);
                        if (item != null)
                        {
                            item.ItemType = itemType;
                            System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] 適切な型で作成成功: {targetType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] 適切な型での作成失敗: {ex.Message}");
                    }
                }

                // フォールバック: BasicCommandItem
                if (item == null)
                {
                    item = new BasicCommandItem();
                    item.ItemType = itemType ?? "Unknown";
                    System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] BasicCommandItemフォールバック: {item.ItemType}");
                }

                // プロパティを復元
                RestorePropertiesFromJson(item, element);

                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] アイテム作成完了: {item.GetType().Name} - ItemType={item.ItemType}, Comment={item.Comment}");
                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] アイテム作成失敗: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] スタックトレース: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// JsonElementからプロパティを復元
        /// </summary>
        private static void RestorePropertiesFromJson(ICommandListItem item, JsonElement element)
        {
            try
            {
                var itemType = item.GetType();

                // 基本プロパティの復元
                if (element.TryGetProperty("comment", out var commentElement) || 
                    element.TryGetProperty("Comment", out commentElement))
                {
                    item.Comment = commentElement.GetString() ?? string.Empty;
                }

                if (element.TryGetProperty("description", out var descElement) || 
                    element.TryGetProperty("Description", out descElement))
                {
                    item.Description = descElement.GetString() ?? string.Empty;
                }

                if (element.TryGetProperty("isEnable", out var isEnableElement) || 
                    element.TryGetProperty("IsEnable", out isEnableElement))
                {
                    item.IsEnable = isEnableElement.GetBoolean();
                }

                if (element.TryGetProperty("lineNumber", out var lineNumberElement) || 
                    element.TryGetProperty("LineNumber", out lineNumberElement))
                {
                    item.LineNumber = lineNumberElement.GetInt32();
                }

                if (element.TryGetProperty("nestLevel", out var nestLevelElement) || 
                    element.TryGetProperty("NestLevel", out nestLevelElement))
                {
                    item.NestLevel = nestLevelElement.GetInt32();
                }

                if (element.TryGetProperty("isInLoop", out var isInLoopElement) || 
                    element.TryGetProperty("IsInLoop", out isInLoopElement))
                {
                    item.IsInLoop = isInLoopElement.GetBoolean();
                }

                if (element.TryGetProperty("isInIf", out var isInIfElement) || 
                    element.TryGetProperty("IsInIf", out isInIfElement))
                {
                    item.IsInIf = isInIfElement.GetBoolean();
                }

                // 型固有のプロパティ復元
                RestoreTypeSpecificProperties(item, element);

                System.Diagnostics.Debug.WriteLine($"[RestorePropertiesFromJson] プロパティ復元完了: {item.ItemType}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestorePropertiesFromJson] プロパティ復元エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 型固有のプロパティを復元
        /// </summary>
        private static void RestoreTypeSpecificProperties(ICommandListItem item, JsonElement element)
        {
            try
            {
                var itemType = item.GetType();

                // リフレクションで各プロパティを復元
                foreach (var property in element.EnumerateObject())
                {
                    var propInfo = itemType.GetProperty(property.Name, 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    
                    if (propInfo == null)
                    {
                        // PascalCase変換を試行
                        var pascalName = char.ToUpper(property.Name[0]) + property.Name.Substring(1);
                        propInfo = itemType.GetProperty(pascalName);
                    }

                    if (propInfo != null && propInfo.CanWrite)
                    {
                        try
                        {
                            object? value = null;
                            
                            // 型に応じた値変換
                            if (propInfo.PropertyType == typeof(string))
                            {
                                value = property.Value.GetString();
                            }
                            else if (propInfo.PropertyType == typeof(int) || propInfo.PropertyType == typeof(int?))
                            {
                                if (property.Value.ValueKind == JsonValueKind.Number)
                                {
                                    value = property.Value.GetInt32();
                                }
                                else if (property.Value.ValueKind == JsonValueKind.String && 
                                         int.TryParse(property.Value.GetString(), out var intVal))
                                {
                                    value = intVal;
                                }
                            }
                            else if (propInfo.PropertyType == typeof(double) || propInfo.PropertyType == typeof(double?))
                            {
                                if (property.Value.ValueKind == JsonValueKind.Number)
                                {
                                    value = property.Value.GetDouble();
                                }
                                else if (property.Value.ValueKind == JsonValueKind.String && 
                                         double.TryParse(property.Value.GetString(), out var doubleVal))
                                {
                                    value = doubleVal;
                                }
                            }
                            else if (propInfo.PropertyType == typeof(bool) || propInfo.PropertyType == typeof(bool?))
                            {
                                if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                                {
                                    value = property.Value.GetBoolean();
                                }
                                else if (property.Value.ValueKind == JsonValueKind.String && 
                                         bool.TryParse(property.Value.GetString(), out var boolVal))
                                {
                                    value = boolVal;
                                }
                            }
                            else if (propInfo.PropertyType.IsEnum)
                            {
                                var stringValue = property.Value.GetString();
                                if (!string.IsNullOrEmpty(stringValue) && 
                                    Enum.TryParse(propInfo.PropertyType, stringValue, true, out var enumValue))
                                {
                                    value = enumValue;
                                }
                            }

                            if (value != null)
                            {
                                propInfo.SetValue(item, value);
                                System.Diagnostics.Debug.WriteLine($"[RestoreTypeSpecificProperties] プロパティ設定成功: {property.Name} = {value}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[RestoreTypeSpecificProperties] プロパティ設定失敗: {property.Name} - {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestoreTypeSpecificProperties] 型固有プロパティ復元エラー: {ex.Message}");
            }
        }
    }
}
