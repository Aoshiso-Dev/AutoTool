using System.Collections.ObjectModel;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Model.List.Class
{
    /// <summary>
    /// コマンドリストの管理サービス（DirectCommandRegistry対応）
    /// UI バインディングは不要なため、ObservableObject は継承しない
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
        /// コンストラクタ（ILogger 依存）
        /// </summary>
        public CommandListService(ILogger<CommandListService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("CommandListService が初期化されました");
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
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Add(item);
            RefreshListState();
            _logger.LogDebug("アイテム追加: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);
        }

        public void Insert(int index, ICommandListItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Insert(index, item);
            RefreshListState();
            _logger.LogDebug("アイテム挿入: {ItemType} (インデックス {Index})", item.ItemType, index);
        }

        public bool Remove(ICommandListItem item)
        {
            if (item == null) return false;

            var result = _items.Remove(item);
            if (result)
            {
                RefreshListState();
                _logger.LogDebug("アイテム削除: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);
            }
            return result;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            var item = _items[index];
            _items.RemoveAt(index);
            RefreshListState();
            _logger.LogDebug("アイテム削除（インデックス）: {ItemType} (インデックス {Index})", item.ItemType, index);
        }

        public void Clear()
        {
            _items.Clear();
            _logger.LogDebug("全アイテムクリア");
        }

        public int IndexOf(ICommandListItem item)
        {
            return _items.IndexOf(item);
        }

        public bool Contains(ICommandListItem item)
        {
            return _items.Contains(item);
        }

        public int Count => _items.Count;

        #endregion

        #region リスト状態管理

        /// <summary>
        /// アイテムの行番号を再計算
        /// </summary>
        private void ReorderItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].LineNumber = i + 1;
            }
        }

        /// <summary>
        /// ネストレベルを計算
        /// </summary>
        private void CalculateNestLevel()
        {
            int currentNestLevel = 0;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                // 終了コマンドの場合、先にネストレベルを下げる
                if (DirectCommandRegistry.IsEndCommand(item.ItemType))
                {
                    currentNestLevel = Math.Max(0, currentNestLevel - 1);
                }

                item.NestLevel = currentNestLevel;

                // 開始コマンドの場合、次のアイテムからネストレベルを上げる
                if (DirectCommandRegistry.IsStartCommand(item.ItemType))
                {
                    currentNestLevel++;
                }
            }
        }

        /// <summary>
        /// If系アイテムのペアリング
        /// </summary>
        private void PairIfItems()
        {
            try
            {
                PairItemsByType(
                    x => DirectCommandRegistry.IsIfCommand(x.ItemType),
                    x => x.ItemType == DirectCommandRegistry.CommandTypes.IfEnd
                );
                _logger.LogTrace("If系アイテムのペアリング完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "If系アイテムのペアリング中にエラー");
                throw;
            }
        }

        /// <summary>
        /// Loop系アイテムのペアリング
        /// </summary>
        private void PairLoopItems()
        {
            try
            {
                PairItemsByType(
                    x => DirectCommandRegistry.IsLoopCommand(x.ItemType),
                    x => x.ItemType == DirectCommandRegistry.CommandTypes.LoopEnd
                );
                _logger.LogTrace("Loop系アイテムのペアリング完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loop系アイテムのペアリング中にエラー");
                throw;
            }
        }

        /// <summary>
        /// 指定条件でアイテムをペアリング
        /// </summary>
        private void PairItemsByType(Func<ICommandListItem, bool> startCondition, Func<ICommandListItem, bool> endCondition)
        {
            var stack = new Stack<ICommandListItem>();

            foreach (var item in _items)
            {
                if (startCondition(item))
                {
                    stack.Push(item);
                }
                else if (endCondition(item))
                {
                    if (stack.Count > 0)
                    {
                        var startItem = stack.Pop();
                        
                        // Pairプロパティの設定（リフレクションを使用）
                        SetPairProperty(startItem, item);
                        SetPairProperty(item, startItem);
                    }
                }
            }
        }

        /// <summary>
        /// Pairプロパティを設定（リフレクション使用）
        /// </summary>
        private void SetPairProperty(ICommandListItem item, ICommandListItem pairItem)
        {
            try
            {
                var pairProperty = item.GetType().GetProperty("Pair");
                if (pairProperty != null && pairProperty.CanWrite)
                {
                    pairProperty.SetValue(item, pairItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pairプロパティの設定に失敗: {ItemType}", item.ItemType);
            }
        }

        #endregion

        #region ファイル操作

        /// <summary>
        /// JSONファイルからコマンドリストを読み込み
        /// </summary>
        public async Task LoadFromFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("ファイル読み込み開始: {FilePath}", filePath);

                var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                var loadedItems = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);

                if (loadedItems == null)
                {
                    throw new InvalidOperationException("ファイル内容が無効です");
                }

                _items.Clear();

                foreach (var itemData in loadedItems)
                {
                    var item = CreateItemFromData(itemData);
                    if (item != null)
                    {
                        _items.Add(item);
                    }
                }

                RefreshListState();
                _logger.LogInformation("ファイル読み込み完了: {Count}個のアイテム", _items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// データからアイテムを作成
        /// </summary>
        private ICommandListItem? CreateItemFromData(Dictionary<string, object> itemData)
        {
            try
            {
                if (!itemData.TryGetValue("ItemType", out var itemTypeObj) || itemTypeObj is not string itemType)
                {
                    return null;
                }

                // UniversalCommandItemとして作成を試行
                if (itemData.TryGetValue("Settings", out var settingsObj) && settingsObj is System.Text.Json.JsonElement settingsElement)
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        // 設定値を復元
                        RestoreSettings(universalItem, settingsElement);
                        RestoreBasicProperties(universalItem, itemData);
                        return universalItem;
                    }
                }

                // フォールバック: BasicCommandItem
                var basicItem = new AutoTool.Model.List.Type.BasicCommandItem();
                basicItem.ItemType = itemType;
                RestoreBasicProperties(basicItem, itemData);
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム作成中にエラー");
                return null;
            }
        }

        private void RestoreSettings(UniversalCommandItem item, System.Text.Json.JsonElement settingsElement)
        {
            try
            {
                var settingsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settingsElement.GetRawText());
                if (settingsDict != null)
                {
                    foreach (var kvp in settingsDict)
                    {
                        item.SetSetting(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "設定値復元中にエラー");
            }
        }

        private void RestoreBasicProperties(ICommandListItem item, Dictionary<string, object> itemData)
        {
            try
            {
                if (itemData.TryGetValue("LineNumber", out var lineNumObj) && lineNumObj is int lineNum)
                    item.LineNumber = lineNum;

                if (itemData.TryGetValue("IsEnable", out var isEnableObj) && isEnableObj is bool isEnable)
                    item.IsEnable = isEnable;

                if (itemData.TryGetValue("Comment", out var commentObj) && commentObj is string comment)
                    item.Comment = comment;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "基本プロパティ復元中にエラー");
            }
        }

        /// <summary>
        /// JSONファイルにコマンドリストを保存
        /// </summary>
        public async Task SaveToFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("ファイル保存開始: {FilePath}", filePath);

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(_items, options);
                await System.IO.File.WriteAllTextAsync(filePath, jsonContent);

                _logger.LogInformation("ファイル保存完了: {Count}個のアイテム", _items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラー: {FilePath}", filePath);
                throw;
            }
        }

        #endregion

        #region ユーティリティ

        /// <summary>
        /// 指定行番号のアイテムを取得
        /// </summary>
        public ICommandListItem? GetItemByLineNumber(int lineNumber)
        {
            return _items.FirstOrDefault(x => x.LineNumber == lineNumber);
        }

        /// <summary>
        /// 次の行番号を取得
        /// </summary>
        public int GetNextLineNumber()
        {
            return _items.Count > 0 ? _items.Max(x => x.LineNumber) + 1 : 1;
        }

        /// <summary>
        /// アイテムを上に移動
        /// </summary>
        public bool MoveUp(ICommandListItem item)
        {
            var index = _items.IndexOf(item);
            if (index > 0)
            {
                _items.RemoveAt(index);
                _items.Insert(index - 1, item);
                RefreshListState();
                return true;
            }
            return false;
        }

        /// <summary>
        /// アイテムを下に移動
        /// </summary>
        public bool MoveDown(ICommandListItem item)
        {
            var index = _items.IndexOf(item);
            if (index >= 0 && index < _items.Count - 1)
            {
                _items.RemoveAt(index);
                _items.Insert(index + 1, item);
                RefreshListState();
                return true;
            }
            return false;
        }

        #endregion
    }
}
