using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5完全統合版：ListPanelViewModel（コマンド管理強化）
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly ObservableCollection<ICommandListItem> _items = new();
        private readonly Stack<CommandListOperation> _undoStack = new();
        private readonly Stack<CommandListOperation> _redoStack = new();

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private int _selectedIndex = -1;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        [ObservableProperty]
        private int _totalItems = 0;

        public ObservableCollection<ICommandListItem> Items => _items;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool HasItems => Items.Count > 0;

        public ListPanelViewModel(ILogger<ListPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            SetupMessaging();
            _logger.LogInformation("Phase 5統合版ListPanelViewModel を初期化しています");

            // コレクション変更の監視
            _items.CollectionChanged += (s, e) =>
            {
                TotalItems = _items.Count;
                HasUnsavedChanges = true;
                UpdateLineNumbers();
            };
        }

        private void SetupMessaging()
        {
            // コマンド操作メッセージの処理
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (r, m) => Add(m.ItemType));
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) => Delete());
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) => MoveUp());
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) => MoveDown());
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) => Clear());
            WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) => Undo());
            WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) => Redo());
            
            // アイテムタイプ変更メッセージの処理
            WeakReferenceMessenger.Default.Register<ChangeItemTypeMessage>(this, (r, m) => ChangeItemType(m.OldItem, m.NewItem));
            
            // リストビュー更新メッセージの処理
            WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (r, m) => RefreshList());
            
            // コマンド実行状態メッセージの処理
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) => OnCommandStarted(m));
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) => OnCommandFinished(m));
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (r, m) => OnProgressUpdated(m));
        }

        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            if (value != null)
            {
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(value));
                _logger.LogDebug("選択アイテム変更: {ItemType} (行 {LineNumber})", value.ItemType, value.LineNumber);
            }
        }

        #region コマンド操作

        [RelayCommand]
        private void Add(string itemType)
        {
            try
            {
                _logger.LogDebug("アイテムを追加します: {ItemType}", itemType);
                var newItem = CreateItem(itemType);
                
                var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                
                // 操作を記録
                var operation = new CommandListOperation
                {
                    Type = OperationType.Add,
                    Index = insertIndex,
                    Item = newItem.Clone(),
                    Description = $"アイテム追加: {itemType}"
                };

                Items.Insert(insertIndex, newItem);
                SelectedIndex = insertIndex;
                SelectedItem = newItem;
                
                RecordOperation(operation);
                StatusMessage = $"{itemType}を追加しました";
                _logger.LogInformation("アイテムを追加しました: {ItemType} (合計 {Count}件)", itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム追加中にエラーが発生しました");
                StatusMessage = $"追加エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedItem == null)
            {
                _logger.LogDebug("削除対象のアイテムが選択されていません");
                StatusMessage = "削除対象が選択されていません";
                return;
            }

            try
            {
                var index = Items.IndexOf(SelectedItem);
                var itemType = SelectedItem.ItemType;
                var itemClone = SelectedItem.Clone();
                
                // 操作を記録
                var operation = new CommandListOperation
                {
                    Type = OperationType.Delete,
                    Index = index,
                    Item = itemClone,
                    Description = $"アイテム削除: {itemType}"
                };

                Items.Remove(SelectedItem);

                if (Items.Count == 0)
                {
                    SelectedIndex = -1;
                    SelectedItem = null;
                }
                else if (index >= Items.Count)
                {
                    SelectedIndex = Items.Count - 1;
                    SelectedItem = Items.LastOrDefault();
                }
                else
                {
                    SelectedIndex = index;
                    SelectedItem = Items.ElementAtOrDefault(index);
                }
                
                RecordOperation(operation);
                
                // 削除後に選択状態の変更を通知
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(SelectedItem));
                
                StatusMessage = $"{itemType}を削除しました";
                _logger.LogInformation("アイテムを削除しました: {ItemType} (残り {Count}件)", itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム削除中にエラーが発生しました");
                StatusMessage = $"削除エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void MoveUp()
        {
            if (SelectedItem == null || SelectedIndex <= 0)
            {
                _logger.LogDebug("上移動できません");
                StatusMessage = "これ以上上に移動できません";
                return;
            }

            try
            {
                var oldIndex = SelectedIndex;
                var newIndex = oldIndex - 1;
                
                // 操作を記録
                var operation = new CommandListOperation
                {
                    Type = OperationType.Move,
                    Index = oldIndex,
                    NewIndex = newIndex,
                    Item = SelectedItem.Clone(),
                    Description = $"アイテム上移動: {SelectedItem.ItemType}"
                };

                Items.Move(oldIndex, newIndex);
                SelectedIndex = newIndex;
                
                RecordOperation(operation);
                StatusMessage = $"{SelectedItem.ItemType}を上に移動しました";
                _logger.LogDebug("アイテムを上に移動しました: {FromIndex} -> {ToIndex}", oldIndex, newIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム上移動中にエラーが発生しました");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void MoveDown()
        {
            if (SelectedItem == null || SelectedIndex >= Items.Count - 1)
            {
                _logger.LogDebug("下移動できません");
                StatusMessage = "これ以上下に移動できません";
                return;
            }

            try
            {
                var oldIndex = SelectedIndex;
                var newIndex = oldIndex + 1;
                
                // 操作を記録
                var operation = new CommandListOperation
                {
                    Type = OperationType.Move,
                    Index = oldIndex,
                    NewIndex = newIndex,
                    Item = SelectedItem.Clone(),
                    Description = $"アイテム下移動: {SelectedItem.ItemType}"
                };

                Items.Move(oldIndex, newIndex);
                SelectedIndex = newIndex;
                
                RecordOperation(operation);
                StatusMessage = $"{SelectedItem.ItemType}を下に移動しました";
                _logger.LogDebug("アイテムを下に移動しました: {FromIndex} -> {ToIndex}", oldIndex, newIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム下移動中にエラーが発生しました");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Clear()
        {
            if (Items.Count == 0)
            {
                StatusMessage = "クリアする項目がありません";
                return;
            }

            try
            {
                var count = Items.Count;
                var itemsClone = Items.Select(item => item.Clone()).ToList();
                
                // 操作を記録
                var operation = new CommandListOperation
                {
                    Type = OperationType.Clear,
                    Items = itemsClone,
                    Description = $"全アイテムクリア ({count}件)"
                };

                Items.Clear();
                SelectedIndex = -1;
                SelectedItem = null;
                
                RecordOperation(operation);
                
                // 全クリア後にEditPanelにnullを通知
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));
                
                StatusMessage = $"全アイテム({count}件)をクリアしました";
                _logger.LogInformation("全アイテムをクリアしました: {Count}件", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムクリア中にエラーが発生しました");
                StatusMessage = $"クリアエラー: {ex.Message}";
            }
        }

        #endregion

        #region Undo/Redo機能

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            if (!CanUndo) return;

            try
            {
                var operation = _undoStack.Pop();
                _redoStack.Push(operation);

                switch (operation.Type)
                {
                    case OperationType.Add:
                        Items.RemoveAt(operation.Index);
                        break;
                    case OperationType.Delete:
                        Items.Insert(operation.Index, operation.Item!);
                        break;
                    case OperationType.Move:
                        Items.Move(operation.NewIndex!.Value, operation.Index);
                        break;
                    case OperationType.Replace:
                        Items[operation.Index] = operation.Item!;
                        SelectedIndex = operation.Index;
                        SelectedItem = operation.Item;
                        break;
                    case OperationType.Clear:
                        foreach (var item in operation.Items!)
                        {
                            Items.Add(item);
                        }
                        break;
                }

                StatusMessage = $"元に戻しました: {operation.Description}";
                _logger.LogDebug("Undo実行: {Description}", operation.Description);
                
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Undo実行中にエラーが発生しました");
                StatusMessage = $"Undoエラー: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            if (!CanRedo) return;

            try
            {
                var operation = _redoStack.Pop();
                _undoStack.Push(operation);

                switch (operation.Type)
                {
                    case OperationType.Add:
                        Items.Insert(operation.Index, operation.Item!);
                        break;
                    case OperationType.Delete:
                        Items.RemoveAt(operation.Index);
                        break;
                    case OperationType.Move:
                        Items.Move(operation.Index, operation.NewIndex!.Value);
                        break;
                    case OperationType.Replace:
                        Items[operation.Index] = operation.NewItem!;
                        SelectedIndex = operation.Index;
                        SelectedItem = operation.NewItem;
                        break;
                    case OperationType.Clear:
                        Items.Clear();
                        break;
                }

                StatusMessage = $"やり直しました: {operation.Description}";
                _logger.LogDebug("Redo実行: {Description}", operation.Description);
                
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redo実行中にエラーが発生しました");
                StatusMessage = $"Redoエラー: {ex.Message}";
            }
        }

        #endregion

        #region アイテムタイプ変更

        /// <summary>
        /// アイテムタイプを変更
        /// </summary>
        private void ChangeItemType(ICommandListItem oldItem, ICommandListItem newItem)
        {
            try
            {
                var index = Items.IndexOf(oldItem);
                if (index >= 0)
                {
                    // 元のアイテムを保存（Undo用）
                    var operation = new CommandListOperation
                    {
                        Type = OperationType.Replace,
                        Index = index,
                        Item = oldItem.Clone(),
                        NewItem = newItem.Clone(),
                        Description = $"タイプ変更: {oldItem.ItemType} -> {newItem.ItemType}"
                    };

                    // アイテムを置換
                    Items[index] = newItem;
                    SelectedIndex = index;
                    SelectedItem = newItem;

                    // 行番号とネストレベルを更新
                    UpdateLineNumbers();

                    RecordOperation(operation);
                    StatusMessage = $"タイプを変更しました: {oldItem.ItemType} -> {newItem.ItemType}";
                    
                    // EditPanelに新しいアイテムを通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));

                    _logger.LogInformation("アイテムタイプを変更しました: {OldType} -> {NewType} (行 {LineNumber})", 
                        oldItem.ItemType, newItem.ItemType, newItem.LineNumber);
                }
                else
                {
                    _logger.LogWarning("変更対象のアイテムが見つかりませんでした");
                    StatusMessage = "変更対象のアイテムが見つかりませんでした";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムタイプ変更中にエラーが発生しました");
                StatusMessage = $"タイプ変更エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// リストビューを強制更新
        /// </summary>
        private void RefreshList()
        {
            try
            {
                // すべてのプロパティ変更通知を発火
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));

                // 各アイテムの描画プロパティを更新
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (item != null)
                    {
                        // ItemTypeプロパティの変更通知を発火してUI更新
                        var currentType = item.ItemType;
                        item.ItemType = currentType; // 同じ値を再設定して通知発火
                    }
                }

                StatusMessage = "リストを更新しました";
                _logger.LogDebug("リストビューを強制更新しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "リスト更新中にエラーが発生しました");
                StatusMessage = $"更新エラー: {ex.Message}";
            }
        }

        #endregion

        #region ヘルパーメソッド

        private ICommandListItem CreateItem(string itemType)
        {
            // CommandRegistryを使用してアイテムを作成
            var itemTypes = CommandRegistry.GetTypeMapping();
            if (itemTypes.TryGetValue(itemType, out var type))
            {
                if (Activator.CreateInstance(type) is ICommandListItem item)
                {
                    item.LineNumber = Items.Count + 1;
                    item.ItemType = itemType;
                    return item;
                }
            }

            // フォールバックとして基本的なアイテムを作成
            return new BasicCommandItem 
            { 
                ItemType = itemType, 
                LineNumber = Items.Count + 1,
                Comment = $"新しい{itemType}コマンド",
                Description = $"{itemType}コマンド"
            };
        }

        private void UpdateLineNumbers()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
            
            // ペアリング処理を追加
            UpdateNestLevel();
            UpdatePairing();
        }

        /// <summary>
        /// ネストレベルを更新
        /// </summary>
        private void UpdateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in Items)
            {
                // ネストレベルを減らすコマンド（終了系）
                if (IsEndCommand(item.ItemType))
                {
                    nestLevel = Math.Max(0, nestLevel - 1);
                }

                item.NestLevel = nestLevel;

                // ネストレベルを増やすコマンド（開始系）
                if (IsStartCommand(item.ItemType))
                {
                    nestLevel++;
                }
            }
        }

        /// <summary>
        /// ペアリングを更新
        /// </summary>
        private void UpdatePairing()
        {
            try
            {
                PairLoopItems();
                PairIfItems();
                _logger.LogDebug("ペアリング更新完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ペアリング更新中にエラーが発生しました");
            }
        }

        /// <summary>
        /// ループアイテムのペアリング
        /// </summary>
        private void PairLoopItems()
        {
            // まず既存のペアリングをクリア
            ClearLoopPairing();

            var loopItems = Items.Where(x => x.ItemType == "Loop").ToList();
            var loopEndItems = Items.Where(x => x.ItemType == "Loop_End").ToList();

            foreach (var loopItem in loopItems)
            {
                // 対応するLoop_Endを探す
                var correspondingEnd = loopEndItems
                    .Where(end => end.LineNumber > loopItem.LineNumber)
                    .Where(end => end.NestLevel == loopItem.NestLevel)
                    .OrderBy(end => end.LineNumber)
                    .FirstOrDefault();

                if (correspondingEnd != null)
                {
                    SetPairProperty(loopItem, correspondingEnd);
                    SetPairProperty(correspondingEnd, loopItem);
                    
                    // LoopCountも同期
                    var loopCount = GetPropertyValue<int>(loopItem, "LoopCount");
                    SetPropertyValue(correspondingEnd, "LoopCount", loopCount);
                    
                    _logger.LogDebug("ループペアリング: Loop({LineNum1}) <-> Loop_End({LineNum2})", 
                        loopItem.LineNumber, correspondingEnd.LineNumber);
                }
                else
                {
                    _logger.LogWarning("Loop (行 {LineNumber}) に対応するLoop_Endが見つかりません", loopItem.LineNumber);
                }
            }
        }

        /// <summary>
        /// IFアイテムのペアリング
        /// </summary>
        private void PairIfItems()
        {
            // まず既存のペアリングをクリア
            ClearIfPairing();

            var ifItems = Items.Where(x => IsIfCommand(x.ItemType)).ToList();
            var ifEndItems = Items.Where(x => x.ItemType == "IF_End").ToList();

            foreach (var ifItem in ifItems)
            {
                // 対応するIF_Endを探す
                var correspondingEnd = ifEndItems
                    .Where(end => end.LineNumber > ifItem.LineNumber)
                    .Where(end => end.NestLevel == ifItem.NestLevel)
                    .OrderBy(end => end.LineNumber)
                    .FirstOrDefault();

                if (correspondingEnd != null)
                {
                    SetPairProperty(ifItem, correspondingEnd);
                    SetPairProperty(correspondingEnd, ifItem);
                    
                    _logger.LogDebug("IFペアリング: {IfType}({LineNum1}) <-> IF_End({LineNum2})", 
                        ifItem.ItemType, ifItem.LineNumber, correspondingEnd.LineNumber);
                }
                else
                {
                    _logger.LogWarning("{IfType} (行 {LineNumber}) に対応するIF_Endが見つかりません", 
                        ifItem.ItemType, ifItem.LineNumber);
                }
            }
        }

        /// <summary>
        /// ループペアリングをクリア
        /// </summary>
        private void ClearLoopPairing()
        {
            foreach (var item in Items.Where(x => x.ItemType == "Loop" || x.ItemType == "Loop_End"))
            {
                SetPairProperty(item, null);
            }
        }

        /// <summary>
        /// IFペアリングをクリア
        /// </summary>
        private void ClearIfPairing()
        {
            foreach (var item in Items.Where(x => IsIfCommand(x.ItemType) || x.ItemType == "IF_End"))
            {
                SetPairProperty(item, null);
            }
        }

        /// <summary>
        /// ペアプロパティを設定
        /// </summary>
        private void SetPairProperty(ICommandListItem item, ICommandListItem? pair)
        {
            var pairProperty = item.GetType().GetProperty("Pair");
            if (pairProperty != null && pairProperty.CanWrite)
            {
                pairProperty.SetValue(item, pair);
            }
        }

        /// <summary>
        /// プロパティ値を取得
        /// </summary>
        private T GetPropertyValue<T>(ICommandListItem item, string propertyName)
        {
            var property = item.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                var value = property.GetValue(item);
                if (value is T tValue)
                    return tValue;
            }
            return default(T)!;
        }

        /// <summary>
        /// プロパティ値を設定
        /// </summary>
        private void SetPropertyValue(ICommandListItem item, string propertyName, object? value)
        {
            var property = item.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(item, value);
            }
        }

        /// <summary>
        /// 開始コマンドかどうかを判定
        /// </summary>
        private bool IsStartCommand(string itemType)
        {
            return itemType switch
            {
                "Loop" => true,
                "IF_ImageExist" => true,
                "IF_ImageNotExist" => true,
                "IF_ImageExist_AI" => true,
                "IF_ImageNotExist_AI" => true,
                "IF_Variable" => true,
                _ => false
            };
        }

        /// <summary>
        /// 終了コマンドかどうかを判定
        /// </summary>
        private bool IsEndCommand(string itemType)
        {
            return itemType switch
            {
                "Loop_End" => true,
                "IF_End" => true,
                _ => false
            };
        }

        /// <summary>
        /// IFコマンドかどうかを判定
        /// </summary>
        private bool IsIfCommand(string itemType)
        {
            return itemType switch
            {
                "IF_ImageExist" => true,
                "IF_ImageNotExist" => true,
                "IF_ImageExist_AI" => true,
                "IF_ImageNotExist_AI" => true,
                "IF_Variable" => true,
                _ => false
            };
        }

        /// <summary>
        /// 操作を記録（Undo/Redo用）
        /// </summary>
        private void RecordOperation(CommandListOperation operation)
        {
            _undoStack.Push(operation);
            _redoStack.Clear(); // 新しい操作が行われたらRedoスタックをクリア
            
            // スタックサイズ制限（メモリ効率）
            const int maxUndoSteps = 100;
            while (_undoStack.Count > maxUndoSteps)
            {
                _undoStack.TryPop(out _);
            }
            
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        #endregion

        #region ファイル操作

        public void Load(string filePath)
        {
            try
            {
                StatusMessage = "ファイル読み込み中...";
                
                if (System.IO.File.Exists(filePath))
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    
                    // 適切な型でデシリアライズ
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    };
                    
                    var itemData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, options);
                    
                    Items.Clear();
                    if (itemData != null)
                    {
                        foreach (var itemDict in itemData)
                        {
                            if (itemDict.TryGetValue("ItemType", out var itemTypeObj) && itemTypeObj is string itemType)
                            {
                                var item = CreateItem(itemType);
                                
                                // プロパティを復元
                                foreach (var kvp in itemDict)
                                {
                                    var property = item.GetType().GetProperty(kvp.Key);
                                    if (property != null && property.CanWrite)
                                    {
                                        try
                                        {
                                            var value = Convert.ChangeType(kvp.Value, property.PropertyType);
                                            property.SetValue(item, value);
                                        }
                                        catch
                                        {
                                            // プロパティ設定失敗は無視
                                        }
                                    }
                                }
                                
                                Items.Add(item);
                            }
                        }
                    }
                }
                
                SelectedIndex = Items.Count > 0 ? 0 : -1;
                SelectedItem = Items.FirstOrDefault();
                UpdateLineNumbers();
                
                // 履歴をクリア
                _undoStack.Clear();
                _redoStack.Clear();
                HasUnsavedChanges = false;
                
                StatusMessage = $"ファイルを読み込みました: {Path.GetFileName(filePath)} ({Items.Count}件)";
                _logger.LogInformation("ファイルを読み込みました: {FilePath} ({Count}件)", filePath, Items.Count);
                
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラーが発生しました: {FilePath}", filePath);
                StatusMessage = $"読み込みエラー: {ex.Message}";
                throw;
            }
        }

        public void Save(string filePath)
        {
            try
            {
                StatusMessage = "ファイル保存中...";
                
                // 保存用のデータを準備
                var saveData = Items.Select(item => new Dictionary<string, object?>
                {
                    ["ItemType"] = item.ItemType,
                    ["LineNumber"] = item.LineNumber,
                    ["Comment"] = item.Comment,
                    ["IsEnable"] = item.IsEnable,
                    ["Description"] = item.Description
                    // 必要に応じて他のプロパティも追加
                }).ToList();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(saveData, options);
                
                var directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                System.IO.File.WriteAllText(filePath, json);
                HasUnsavedChanges = false;
                
                StatusMessage = $"ファイルを保存しました: {Path.GetFileName(filePath)} ({Items.Count}件)";
                _logger.LogInformation("ファイルを保存しました: {FilePath} ({Count}件)", filePath, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラーが発生しました: {FilePath}", filePath);
                StatusMessage = $"保存エラー: {ex.Message}";
                throw;
            }
        }

        #endregion

        #region 検証・統計

        /// <summary>
        /// コマンドリストの検証
        /// </summary>
        [RelayCommand]
        private void ValidateCommands()
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    
                    // 基本検証
                    if (string.IsNullOrEmpty(item.ItemType))
                    {
                        errors.Add($"行 {i + 1}: アイテムタイプが設定されていません");
                    }

                    // ペア検証（Loop、If系）
                    if (item.ItemType.StartsWith("Loop") && !item.ItemType.EndsWith("_End") && !item.ItemType.EndsWith("_Break"))
                    {
                        // TODO: ペア検証ロジック
                    }
                }

                if (errors.Count > 0)
                {
                    StatusMessage = $"検証エラー: {errors.Count}件";
                    WeakReferenceMessenger.Default.Send(new ValidationMessage("Error", string.Join("\n", errors), true));
                }
                else if (warnings.Count > 0)
                {
                    StatusMessage = $"検証警告: {warnings.Count}件";
                    WeakReferenceMessenger.Default.Send(new ValidationMessage("Warning", string.Join("\n", warnings)));
                }
                else
                {
                    StatusMessage = "検証完了: 問題ありません";
                    WeakReferenceMessenger.Default.Send(new ValidationMessage("Success", "コマンドリストに問題はありません"));
                }

                _logger.LogInformation("コマンドリスト検証完了: エラー{ErrorCount}件, 警告{WarningCount}件", errors.Count, warnings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドリスト検証中にエラーが発生しました");
                StatusMessage = $"検証エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// 統計情報の取得
        /// </summary>
        public CommandListStats GetStats()
        {
            var stats = new CommandListStats
            {
                TotalItems = Items.Count,
                EnabledItems = Items.Count(i => i.IsEnable),
                DisabledItems = Items.Count(i => !i.IsEnable),
                ItemTypeStats = Items.GroupBy(i => i.ItemType).ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }

        #endregion

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            StatusMessage = isRunning ? "実行中..." : "準備完了";
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        /// <summary>
        /// 準備処理
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger.LogDebug("ListPanelViewModel の準備処理を実行します");
                
                // 実行関連の状態をリセット
                foreach (var item in Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }
                
                StatusMessage = "準備完了";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanelViewModel 準備処理中にエラーが発生しました");
            }
        }

        #region コマンド実行状態処理

        /// <summary>
        /// コマンド開始処理
        /// </summary>
        private void OnCommandStarted(StartCommandMessage message)
        {
            try
            {
                // LineNumberでアイテムを特定してIsRunning=trueに設定
                var item = Items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                if (item != null)
                {
                    item.IsRunning = true;
                    item.Progress = 0;
                    _logger.LogDebug("コマンド開始: Line {LineNumber} - {ItemType}", item.LineNumber, item.ItemType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド開始処理中にエラーが発生しました");
            }
        }

        /// <summary>
        /// コマンド完了処理
        /// </summary>
        private void OnCommandFinished(FinishCommandMessage message)
        {
            try
            {
                // LineNumberでアイテムを特定してIsRunning=falseに設定
                var item = Items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                if (item != null)
                {
                    item.IsRunning = false;
                    item.Progress = 100; // 完了時は100%
                    _logger.LogDebug("コマンド完了: Line {LineNumber} - {ItemType}", item.LineNumber, item.ItemType);
                    
                    // 少し遅延してProgressをクリア
                    Task.Delay(1000).ContinueWith(_ => 
                    {
                        if (!item.IsRunning) // まだ実行中でなければクリア
                        {
                            item.Progress = 0;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド完了処理中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 進捗更新処理
        /// </summary>
        private void OnProgressUpdated(UpdateProgressMessage message)
        {
            try
            {
                // LineNumberでアイテムを特定してProgressを更新
                var item = Items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                if (item != null && item.IsRunning)
                {
                    item.Progress = message.Progress;
                    _logger.LogTrace("進捗更新: Line {LineNumber} - {Progress}%", item.LineNumber, message.Progress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "進捗更新処理中にエラーが発生しました");
            }
        }

        #endregion
    }

    #region 補助クラス

    /// <summary>
    /// コマンドリスト操作の記録
    /// </summary>
    public class CommandListOperation
    {
        public OperationType Type { get; set; }
        public int Index { get; set; }
        public int? NewIndex { get; set; }
        public ICommandListItem? Item { get; set; }
        public ICommandListItem? NewItem { get; set; }
        public List<ICommandListItem>? Items { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 操作タイプ
    /// </summary>
    public enum OperationType
    {
        Add,
        Delete,
        Move,
        Clear,
        Edit,
        Replace
    }

    /// <summary>
    /// コマンドリスト統計
    /// </summary>
    public class CommandListStats
    {
        public int TotalItems { get; set; }
        public int EnabledItems { get; set; }
        public int DisabledItems { get; set; }
        public Dictionary<string, int> ItemTypeStats { get; set; } = new();
    }

    #endregion
}