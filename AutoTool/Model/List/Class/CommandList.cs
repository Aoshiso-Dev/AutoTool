using System.Collections.ObjectModel;
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
    /// コマンドリストの管理サービス（UniversalCommandItem専用）
    /// UI バインディングは不要なため、ObservableObject は継承しない
    /// </summary>
    public class CommandListService
    {
        private readonly ILogger<CommandListService> _logger;
        private readonly ObservableCollection<UniversalCommandItem> _items = new();

        /// <summary>
        /// アイテムコレクション（読み取り専用プロパティ）
        /// </summary>
        public ObservableCollection<UniversalCommandItem> Items => _items;

        /// <summary>
        /// インデクサ
        /// </summary>
        public UniversalCommandItem this[int index]
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

        public void Add(UniversalCommandItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Add(item);
            RefreshListState();
            _logger.LogDebug("アイテム追加: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);
        }

        public void Insert(int index, UniversalCommandItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Insert(index, item);
            RefreshListState();
            _logger.LogDebug("アイテム挿入: {ItemType} (インデックス {Index})", item.ItemType, index);
        }

        public bool Remove(UniversalCommandItem item)
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

        public int IndexOf(UniversalCommandItem item)
        {
            return _items.IndexOf(item);
        }

        public bool Contains(UniversalCommandItem item)
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
        /// 指定条件でアイテムをペアリング（UniversalCommandItem専用）
        /// </summary>
        private void PairItemsByType(Func<UniversalCommandItem, bool> startCondition, Func<UniversalCommandItem, bool> endCondition)
        {
            var stack = new Stack<UniversalCommandItem>();

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
                        
                        // Pairプロパティの設定（UniversalCommandItemなので直接アクセス可能）
                        startItem.Pair = item;
                        item.Pair = startItem;
                    }
                }
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
        /// データからUniversalCommandItemを作成
        /// </summary>
        private UniversalCommandItem? CreateItemFromData(Dictionary<string, object> itemData)
        {
            try
            {
                var itemType = itemData.TryGetValue("ItemType", out var typeValue) 
                    ? typeValue?.ToString() ?? "" 
                    : "";

                _logger.LogDebug("アイテム作成開始: {ItemType}", itemType);

                // 1. DirectCommandRegistryを使用してUniversalCommandItem作成を試行
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null && itemData.TryGetValue("Settings", out var settingsObj) && settingsObj is System.Text.Json.JsonElement settingsElement)
                    {
                        // 設定値を復元
                        RestoreSettings(universalItem, settingsElement);
                        RestoreBasicProperties(universalItem, itemData);
                        return universalItem;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "UniversalCommandItem作成失敗、フォールバックへ: {ItemType}", itemType);
                }

                // フォールバック: UniversalCommandItem
                var basicItem = new UniversalCommandItem();
                basicItem.ItemType = itemType;
                RestoreBasicProperties(basicItem, itemData);
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム作成時にエラー");
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
                _logger.LogWarning(ex, "設定値復元時にエラー");
            }
        }

        private void RestoreBasicProperties(UniversalCommandItem item, Dictionary<string, object> itemData)
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
        public UniversalCommandItem? GetItemByLineNumber(int lineNumber)
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
        public bool MoveUp(UniversalCommandItem item)
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
        public bool MoveDown(UniversalCommandItem item)
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
