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
using AutoTool.List.Class; // CommandListクラス用
using System.Windows; // Thickness用
using System.Threading.Tasks; // Task用
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5完全統合版：ListPanelViewModel（DI対応版）
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ObservableCollection<ICommandListItem> _items = new();
        private readonly Stack<CommandListOperation> _undoStack = new();
        private readonly Stack<CommandListOperation> _redoStack = new();
        private int _completedCommands = 0;

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

        // プログレスバー関連プロパティ
        [ObservableProperty]
        private int _totalProgress = 0;

        [ObservableProperty]
        private int _currentProgress = 0;

        [ObservableProperty]
        private string _progressText = "";

        [ObservableProperty]
        private bool _showProgress = false;

        [ObservableProperty]
        private ICommandListItem? _currentExecutingItem;

        partial void OnCurrentExecutingItemChanged(ICommandListItem? value)
        {
            try
            {
                _logger.LogDebug("CurrentExecutingItem変更: {OldItem} -> {NewItem}", 
                    _currentExecutingItem?.ItemType ?? "null", 
                    value?.ItemType ?? "null");
                
                // 関連プロパティの更新通知
                OnPropertyChanged(nameof(CurrentExecutingDescription));
                
                // UIの強制更新
                OnPropertyChanged(nameof(Items));
                
                _logger.LogDebug("CurrentExecutingItem変更完了: {Description}", CurrentExecutingDescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CurrentExecutingItem変更処理中にエラー");
            }
        }

        public ObservableCollection<ICommandListItem> Items => _items;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool HasItems => Items.Count > 0;

        /// <summary>
        /// プログレス率を取得（パーセンテージ）
        /// </summary>
        public double ProgressPercentage => TotalProgress > 0 ? (double)CurrentProgress / TotalProgress * 100 : 0;

        /// <summary>
        /// 残り時間の推定（簡易版）
        /// </summary>
        public string EstimatedTimeRemaining
        {
            get
            {
                if (TotalProgress <= 0 || CurrentProgress <= 0) return "不明";
                
                var remaining = TotalProgress - CurrentProgress;
                if (remaining <= 0) return "完了";
                
                // 簡易的な推定（実際の実装では実行時間を記録する必要あり）
                var estimatedSeconds = remaining * 2; // 1コマンドあたり2秒と仮定
                return $"約{estimatedSeconds}秒";
            }
        }

        /// <summary>
        /// 現在実行中のコマンドの説明を取得
        /// </summary>
        public string CurrentExecutingDescription
        {
            get
            {
                if (CurrentExecutingItem == null) return "";
                
                var displayName = CommandRegistry.DisplayOrder.GetDisplayName(CurrentExecutingItem.ItemType) ?? CurrentExecutingItem.ItemType;
                var comment = !string.IsNullOrEmpty(CurrentExecutingItem.Comment) ? $"({CurrentExecutingItem.Comment})" : "";
                return $"実行中: {displayName} {comment}";
            }
        }

        /// <summary>
        /// DI対応コンストラクタ
        /// </summary>
        public ListPanelViewModel(ILogger<ListPanelViewModel> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            SetupMessaging();
            _logger.LogInformation("DI対応ListPanelViewModel を初期化しています");

            // コレクション変更の監視
            _items.CollectionChanged += (s, e) =>
            {
                TotalItems = _items.Count;
                HasUnsavedChanges = true;
                
                // プロパティ変更通知の強化
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                
                // MainWindowViewModelにアイテム数変更を通知
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(_items.Count));
                
                // コレクション変更後に少し遅延してペアリング更新
                Task.Delay(50).ContinueWith(_ =>
                {
                    try
                    {
                        UpdateLineNumbers();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "コレクション変更後のペアリング更新でエラー");
                    }
                }, TaskScheduler.Default);
            };
        }

        private void SetupMessaging()
        {
            // コマンド操作メッセージの処理
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (r, m) => AddInternal(m.ItemType));
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) => DeleteInternal());
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) => MoveUpInternal());
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) => MoveDownInternal());
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) => ClearInternal());
            WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) => UndoInternal());
            WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) => RedoInternal());
            
            // アイテムタイプ変更メッセージの処理
            WeakReferenceMessenger.Default.Register<ChangeItemTypeMessage>(this, (r, m) => ChangeItemType(m.OldItem, m.NewItem));
            
            // リストビュー更新メッセージの処理
            WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (r, m) => RefreshList());
            
            // コマンド実行状態メッセージの処理
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) => OnCommandStarted(m));
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) => OnCommandFinished(m));
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (r, m) => OnProgressUpdated(m));
            
            // ファイル操作メッセージの処理
            WeakReferenceMessenger.Default.Register<LoadFileMessage>(this, (r, m) => LoadFileInternal(m.FilePath));
            WeakReferenceMessenger.Default.Register<SaveFileMessage>(this, (r, m) => SaveFileInternal(m.FilePath));
            
            // マクロ実行状態メッセージの処理
            WeakReferenceMessenger.Default.Register<MacroExecutionStateMessage>(this, (r, m) => SetRunningState(m.IsRunning));
        }

        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            if (value != null)
            {
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(value));
                _logger.LogDebug("選択アイテム変更: {ItemType} (行 {LineNumber})", value.ItemType, value.LineNumber);
            }
        }

        #region 外部からアクセス可能なメソッド

        /// <summary>
        /// 外部からアクセス可能なアイテム追加メソッド
        /// </summary>
        public void Add(string itemType)
        {
            try
            {
                _logger.LogDebug("外部Add呼び出し: {ItemType}", itemType);
                AddInternal(itemType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "外部Add呼び出し中にエラー: {ItemType}", itemType);
            }
        }

        public void Delete() => DeleteInternal();
        public void MoveUp() => MoveUpInternal();
        public void MoveDown() => MoveDownInternal();
        public void Clear() => ClearInternal();
        public void Undo() => UndoInternal();
        public void Redo() => RedoInternal();

        #endregion

        #region 内部実装メソッド

        [RelayCommand]
        private void AddInternal(string itemType)
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

                _logger.LogDebug("アイテム追加前のItems数: {Count}", Items.Count);
                
                Items.Insert(insertIndex, newItem);
                
                _logger.LogDebug("アイテム追加後のItems数: {Count}", Items.Count);
                
                SelectedIndex = insertIndex;
                SelectedItem = newItem;
                
                RecordOperation(operation);
                StatusMessage = $"{itemType}を追加しました";
                
                // プロパティ変更通知を明示的に発火
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));
                
                // MainWindowViewModelにアイテム数変更を通知
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                
                // MainWindowViewModelに選択変更を通知
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));
                
                _logger.LogInformation("アイテムを追加しました: {ItemType} (合計 {Count}件)", itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム追加中にエラーが発生しました");
                StatusMessage = $"追加エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void DeleteInternal()
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
        private void MoveUpInternal()
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
        private void MoveDownInternal()
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
        private void ClearInternal()
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

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoInternal()
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
        private void RedoInternal()
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

        #region プログレス管理

        /// <summary>
        /// マクロ実行開始時のプログレス初期化
        /// </summary>
        public void InitializeProgress()
        {
            try
            {
                TotalProgress = Items.Count(i => i.IsEnable);
                CurrentProgress = 0;
                ShowProgress = TotalProgress > 0;
                ProgressText = ShowProgress ? $"0 / {TotalProgress}" : "";
                CurrentExecutingItem = null;
                
                _logger.LogDebug("プログレス初期化: 総数={TotalProgress}", TotalProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プログレス初期化中にエラー");
            }
        }

        /// <summary>
        /// プログレス更新
        /// </summary>
        public void UpdateProgress(int completed)
        {
            try
            {
                CurrentProgress = Math.Min(completed, TotalProgress);
                ProgressText = $"{CurrentProgress} / {TotalProgress}";
                
                if (CurrentProgress >= TotalProgress)
                {
                    ShowProgress = false;
                    ProgressText = "完了";
                    CurrentExecutingItem = null;
                }
                
                _logger.LogTrace("プログレス更新: {Current}/{Total}", CurrentProgress, TotalProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プログレス更新中にエラー");
            }
        }

        /// <summary>
        /// プログレス完了処理
        /// </summary>
        public void CompleteProgress()
        {
            try
            {
                CurrentProgress = TotalProgress;
                ShowProgress = false;
                ProgressText = "完了";
                CurrentExecutingItem = null;
                
                _logger.LogDebug("プログレス完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プログレス完了処理中にエラー");
            }
        }

        /// <summary>
        /// プログレス中断処理
        /// </summary>
        public void CancelProgress()
        {
            try
            {
                ShowProgress = false;
                ProgressText = "中断";
                CurrentExecutingItem = null;
                
                _logger.LogDebug("プログレス中断");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プログレス中断処理中にエラー");
            }
        }

        #endregion

        #region ファイル操作（DI対応）

        /// <summary>
        /// メッセージからのファイル読み込み
        /// </summary>
        private void LoadFileInternal(string filePath)
        {
            try
            {
                Load(filePath);
                // MainWindowViewModelにアイテム数変更を通知
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                // ログメッセージを送信
                WeakReferenceMessenger.Default.Send(new LogMessage("Info", $"ファイル読み込み完了: {Path.GetFileName(filePath)} ({Items.Count}件)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラー: {FilePath}", filePath);
                WeakReferenceMessenger.Default.Send(new LogMessage("Error", $"ファイル読み込み失敗: {ex.Message}"));
            }
        }

        /// <summary>
        /// メッセージからのファイル保存
        /// </summary>
        private void SaveFileInternal(string filePath)
        {
            try
            {
                Save(filePath);
                // ログメッセージを送信
                WeakReferenceMessenger.Default.Send(new LogMessage("Info", $"ファイル保存完了: {Path.GetFileName(filePath)} ({Items.Count}件)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラー: {FilePath}", filePath);
                WeakReferenceMessenger.Default.Send(new LogMessage("Error", $"ファイル保存失敗: {ex.Message}"));
            }
        }

        public void Load(string filePath)
        {
            try
            {
                StatusMessage = "ファイル読み込み中...";
                
                if (System.IO.File.Exists(filePath))
                {
                    // CommandListをDIから取得
                    var commandListService = _serviceProvider.GetService<CommandList>();
                    if (commandListService == null)
                    {
                        // フォールバック：直接作成
                        commandListService = new CommandList();
                        _logger.LogWarning("CommandListサービスが見つからないため、直接作成しました");
                    }
                    
                    commandListService.Load(filePath);
                    
                    _logger.LogInformation("ファイル読み込み成功: {Count}件", commandListService.Items.Count);
                    
                    // ObservableCollectionを破壊せずに内容を更新
                    Items.Clear();
                    foreach (var item in commandListService.Items)
                    {
                        Items.Add(item);
                    }
                    
                    SelectedIndex = Items.Count > 0 ? 0 : -1;
                    SelectedItem = Items.FirstOrDefault();
                    
                    HasUnsavedChanges = false;
                    StatusMessage = $"ファイルを読み込みました: {Path.GetFileName(filePath)} ({Items.Count}件)";
                    _logger.LogInformation("ファイルを読み込みました: {FilePath} ({Count}件)", filePath, Items.Count);
                }
                else
                {
                    _logger.LogWarning("ファイルが存在しません: {FilePath}", filePath);
                    Items.Clear();
                }
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
                
                // JsonSerializerOptionsをDIから取得
                var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
                if (jsonOptions == null)
                {
                    // フォールバック：デフォルト設定
                    jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    _logger.LogWarning("JsonSerializerOptionsサービスが見つからないため、デフォルト設定を使用しました");
                }
                
                // 保存用のデータを準備
                var saveData = Items.Select(item => new Dictionary<string, object?>
                {
                    ["ItemType"] = item.ItemType,
                    ["LineNumber"] = item.LineNumber,
                    ["Comment"] = item.Comment,
                    ["IsEnable"] = item.IsEnable,
                    ["Description"] = item.Description
                }).ToList();
                
                var json = System.Text.Json.JsonSerializer.Serialize(saveData, jsonOptions);
                
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

        #region ヘルパーメソッド（DI対応）

        private ICommandListItem CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("CreateItem開始: {ItemType}", itemType);
                
                // FactoryパターンでCommandListItemを作成
                var factory = _serviceProvider.GetService<AutoTool.Services.ICommandListItemFactory>();
                if (factory != null)
                {
                    var item = factory.CreateItem(itemType);
                    if (item != null)
                    {
                        item.LineNumber = Items.Count + 1;
                        if (string.IsNullOrEmpty(item.Comment))
                        {
                            item.Comment = $"新しい{itemType}コマンド";
                        }
                        if (string.IsNullOrEmpty(item.Description))
                        {
                            item.Description = $"{itemType}コマンド";
                        }
                        
                        _logger.LogDebug("Factoryで作成成功: {ActualType}", item.GetType().Name);
                        return item;
                    }
                }
                
                _logger.LogWarning("Factoryが見つからないか作成失敗、フォールバック実行: {ItemType}", itemType);
                
                // フォールバック：CommandRegistryを直接使用
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (itemTypes.TryGetValue(itemType, out var type))
                {
                    _logger.LogDebug("CommandRegistryからタイプ取得: {Type}", type.Name);
                    
                    // DIコンテナからインスタンスを取得を試みる
                    var serviceInstance = _serviceProvider.GetService(type);
                    if (serviceInstance is ICommandListItem item)
                    {
                        item.LineNumber = Items.Count + 1;
                        item.ItemType = itemType;
                        item.IsEnable = true;
                        if (string.IsNullOrEmpty(item.Comment))
                        {
                            item.Comment = $"新しい{itemType}コマンド";
                        }
                        if (string.IsNullOrEmpty(item.Description))
                        {
                            item.Description = $"{itemType}コマンド";
                        }
                        
                        _logger.LogDebug("DIコンテナで作成成功: {ActualType}", item.GetType().Name);
                        return item;
                    }
                    
                    // DIで取得できない場合はActivatorで作成
                    if (Activator.CreateInstance(type) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.LineNumber = Items.Count + 1;
                        fallbackItem.ItemType = itemType;
                        fallbackItem.IsEnable = true;
                        if (string.IsNullOrEmpty(fallbackItem.Comment))
                        {
                            fallbackItem.Comment = $"新しい{itemType}コマンド";
                        }
                        if (string.IsNullOrEmpty(fallbackItem.Description))
                        {
                            fallbackItem.Description = $"{itemType}コマンド";
                        }
                        
                        _logger.LogDebug("Activatorで作成成功: {ActualType}", fallbackItem.GetType().Name);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("CommandRegistryで作成失敗、BasicCommandItemで代替: {ItemType}", itemType);

                // 最終フォールバック：BasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItemもDIから取得できないため、直接作成しました");
                }
                
                basicItem.ItemType = itemType;
                basicItem.LineNumber = Items.Count + 1;
                basicItem.Comment = $"新しい{itemType}コマンド";
                basicItem.Description = $"{itemType}コマンド";
                basicItem.IsEnable = true;
                
                _logger.LogDebug("BasicCommandItemで作成完了");
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateItem中にエラーが発生: {ItemType}", itemType);
                
                // 緊急フォールバック
                return new BasicCommandItem 
                { 
                    ItemType = itemType, 
                    LineNumber = Items.Count + 1,
                    Comment = $"エラー復旧: {itemType}",
                    Description = "エラーから復旧したアイテム",
                    IsEnable = true
                };
            }
        }

        private void UpdateLineNumbers()
        {
            try
            {
                if (_items.Count == 0)
                {
                    _logger.LogDebug("アイテムが0件のため、ペアリング処理をスキップ");
                    return;
                }

                _logger.LogDebug("行番号・ネストレベル・ペアリング更新開始: {Count}件", _items.Count);

                // Step 1: 行番号を更新
                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].LineNumber = i + 1;
                }

                // Step 2: ネストレベルを計算
                UpdateNestLevelInternal();

                // Step 3: Pairプロパティをクリア
                ClearAllPairs();

                // Step 4: ペアリングを実行
                UpdatePairingInternal();

                _logger.LogDebug("行番号・ネストレベル・ペアリング更新完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "行番号更新中にエラーが発生しました: {Message}", ex.Message);
                
                // フォールバック：最低限の行番号更新
                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].LineNumber = i + 1;
                }
            }
        }

        private void UpdateNestLevelInternal()
        {
            try
            {
                var nestLevel = 0;
                var loopStack = new Stack<ICommandListItem>(); // Loop開始コマンドのスタック
                var ifStack = new Stack<ICommandListItem>(); // If開始コマンドのスタック

                foreach (var item in _items)
                {
                    // 終了コマンドの場合、対応する開始コマンドを探してネストレベルを調整
                    if (item.ItemType == "Loop_End")
                    {
                        if (loopStack.Count > 0)
                        {
                            loopStack.Pop();
                            nestLevel = Math.Max(0, nestLevel - 1);
                        }
                    }
                    else if (item.ItemType == "IF_End")
                    {
                        if (ifStack.Count > 0)
                        {
                            ifStack.Pop();
                            nestLevel = Math.Max(0, nestLevel - 1);
                        }
                    }

                    // 現在のアイテムのネストレベルを設定
                    item.NestLevel = nestLevel;
                    
                    // ネスト内にいることを示すフラグを設定
                    item.IsInLoop = loopStack.Count > 0;
                    item.IsInIf = ifStack.Count > 0;

                    // 開始コマンドの場合、スタックに積んでネストレベルを増加
                    if (item.ItemType == "Loop")
                    {
                        loopStack.Push(item);
                        nestLevel++;
                    }
                    else if (IsIfCommand(item.ItemType))
                    {
                        ifStack.Push(item);
                        nestLevel++;
                    }
                }

                // 残った開始コマンドがあれば警告
                if (loopStack.Count > 0)
                {
                    _logger.LogWarning("対応するLoop_Endが見つからないLoopコマンドが{Count}個あります", loopStack.Count);
                }
                if (ifStack.Count > 0)
                {
                    _logger.LogWarning("対応するIF_Endが見つからないIfコマンドが{Count}個あります", ifStack.Count);
                }

                _logger.LogDebug("改良版ネストレベル計算完了: 最大ネスト{MaxLevel}", nestLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ネストレベル計算中にエラー: {Message}", ex.Message);
            }
        }

        private void ClearAllPairs()
        {
            try
            {
                foreach (var item in _items)
                {
                    SetPairProperty(item, null);
                }
                _logger.LogDebug("全ペアプロパティクリア完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ペアプロパティクリア中にエラー: {Message}", ex.Message);
            }
        }

        private void UpdatePairingInternal()
        {
            try
            {
                // Loopペアリング
                var loopItems = _items.Where(x => x.ItemType == "Loop").ToList();
                var loopEndItems = _items.Where(x => x.ItemType == "Loop_End").ToList();

                foreach (var loopItem in loopItems)
                {
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
                        if (loopCount > 0)
                        {
                            SetPropertyValue(correspondingEnd, "LoopCount", loopCount);
                        }
                        
                        _logger.LogDebug("ループペアリング成功: Loop({LoopLine}) <-> Loop_End({EndLine})", 
                            loopItem.LineNumber, correspondingEnd.LineNumber);
                    }
                    else
                    {
                        _logger.LogWarning("Loop (行 {LineNumber}) に対応するLoop_Endが見つかりません", loopItem.LineNumber);
                    }
                }

                // Ifペアリング
                var ifItems = _items.Where(x => IsIfCommand(x.ItemType)).ToList();
                var ifEndItems = _items.Where(x => x.ItemType == "IF_End").ToList();

                foreach (var ifItem in ifItems)
                {
                    var correspondingEnd = ifEndItems
                        .Where(end => end.LineNumber > ifItem.LineNumber)
                        .Where(end => end.NestLevel == ifItem.NestLevel)
                        .OrderBy(end => end.LineNumber)
                        .FirstOrDefault();

                    if (correspondingEnd != null)
                    {
                        SetPairProperty(ifItem, correspondingEnd);
                        SetPairProperty(correspondingEnd, ifItem);
                        
                        _logger.LogDebug("Ifペアリング成功: {IfType}({IfLine}) <-> IF_End({EndLine})", 
                            ifItem.ItemType, ifItem.LineNumber, correspondingEnd.LineNumber);
                    }
                    else
                    {
                        _logger.LogWarning("{IfType} (行 {LineNumber}) に対応するIF_Endが見つかりません", 
                            ifItem.ItemType, ifItem.LineNumber);
                    }
                }

                _logger.LogDebug("ペアリング処理完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ペアリング処理中にエラー: {Message}", ex.Message);
            }
        }

        private void SetPairProperty(ICommandListItem item, ICommandListItem? pair)
        {
            try
            {
                var pairProperty = item.GetType().GetProperty("Pair");
                if (pairProperty != null && pairProperty.CanWrite)
                {
                    pairProperty.SetValue(item, pair);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ペアプロパティ設定に失敗: {ItemType}", item.ItemType);
            }
        }

        private T GetPropertyValue<T>(ICommandListItem item, string propertyName)
        {
            try
            {
                var property = item.GetType().GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(item);
                    if (value is T tValue)
                        return tValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "プロパティ値取得に失敗: {PropertyName} on {ItemType}", propertyName, item.ItemType);
            }
            return default(T)!;
        }

        private void SetPropertyValue(ICommandListItem item, string propertyName, object? value)
        {
            try
            {
                var property = item.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(item, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "プロパティ値設定に失敗: {PropertyName} on {ItemType}", propertyName, item.ItemType);
            }
        }

        private bool IsStartCommand(string itemType) => itemType switch
        {
            "Loop" => true,
            "IF_ImageExist" => true,
            "IF_ImageNotExist" => true,
            "IF_ImageExist_AI" => true,
            "IF_ImageNotExist_AI" => true,
            "IF_Variable" => true,
            _ => false
        };

        private bool IsEndCommand(string itemType) => itemType switch
        {
            "Loop_End" => true,
            "IF_End" => true,
            _ => false
        };

        private bool IsIfCommand(string itemType) => itemType switch
        {
            "IF_ImageExist" => true,
            "IF_ImageNotExist" => true,
            "IF_ImageExist_AI" => true,
            "IF_ImageNotExist_AI" => true,
            "IF_Variable" => true,
            _ => false
        };

        private void RecordOperation(CommandListOperation operation)
        {
            _undoStack.Push(operation);
            _redoStack.Clear();
            
            const int maxUndoSteps = 100;
            while (_undoStack.Count > maxUndoSteps)
            {
                _undoStack.TryPop(out _);
            }
            
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        #endregion

        #region コマンド実行状態管理

        private void OnCommandStarted(StartCommandMessage message)
        {
            try
            {
                // より柔軟な検索ロジック
                var item = FindMatchingItem(message.LineNumber, message.ItemType);
                
                if (item != null)
                {
                    item.IsRunning = true;
                    item.Progress = 0;
                    CurrentExecutingItem = item;
                    
                    _logger.LogDebug("コマンド開始: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})", 
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);
                }
                else
                {
                    _logger.LogWarning("コマンド開始: 対応するアイテムが見つかりません - Line {MessageLineNumber}, Type {MessageItemType}", 
                        message.LineNumber, message.ItemType);
                    
                    // デバッグ情報を出力
                    _logger.LogDebug("現在のアイテム一覧:");
                    foreach (var debugItem in Items.Take(10))
                    {
                        _logger.LogDebug("  Line {LineNumber}: {ItemType} (IsEnable: {IsEnable})", 
                            debugItem.LineNumber, debugItem.ItemType, debugItem.IsEnable);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド開始処理中にエラーが発生しました");
            }
        }

        private void OnCommandFinished(FinishCommandMessage message)
        {
            try
            {
                var item = FindMatchingItem(message.LineNumber, message.ItemType);
                
                if (item != null)
                {
                    item.IsRunning = false;
                    item.Progress = 100;
                    
                    if (item.IsEnable)
                    {
                        _completedCommands++;
                        UpdateProgress(_completedCommands);
                    }
                    
                    _logger.LogDebug("コマンド完了: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})", 
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);
                    
                    // CurrentExecutingItemをクリア
                    if (CurrentExecutingItem == item)
                    {
                        CurrentExecutingItem = null;
                    }
                    
                    Task.Delay(1000).ContinueWith(_ => 
                    {
                        if (!item.IsRunning)
                        {
                            item.Progress = 0;
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("コマンド完了: 対応するアイテムが見つかりません - Line {MessageLineNumber}, Type {MessageItemType}", 
                        message.LineNumber, message.ItemType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド完了処理中にエラーが発生しました");
            }
        }

        private void OnProgressUpdated(UpdateProgressMessage message)
        {
            try
            {
                var item = FindMatchingItem(message.LineNumber, message.ItemType);
                
                if (item != null && item.IsRunning)
                {
                    item.Progress = message.Progress;
                    
                    _logger.LogTrace("進捗更新: Line {LineNumber} ({MessageLineNumber}) - {Progress}% - {ItemType} ({MessageItemType})", 
                        item.LineNumber, message.LineNumber, message.Progress, item.ItemType, message.ItemType);
                }
                else if (item == null)
                {
                    _logger.LogTrace("進捗更新: 対応するアイテムが見つかりません - Line {MessageLineNumber}, Type {MessageItemType}", 
                        message.LineNumber, message.ItemType);
                }
                else if (!item.IsRunning)
                {
                    _logger.LogTrace("進捗更新: アイテムが実行中ではありません - Line {LineNumber}, Type {ItemType}, IsRunning: {IsRunning}", 
                        item.LineNumber, item.ItemType, item.IsRunning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "進捗更新処理中にエラーが発生しました");
            }
        }

        /// <summary>
        /// メッセージに対応するアイテムを検索
        /// </summary>
        private ICommandListItem? FindMatchingItem(int messageLineNumber, string messageItemType)
        {
            // 1. 正確な一致を優先
            var exactMatch = Items.FirstOrDefault(x => 
                x.LineNumber == messageLineNumber && x.ItemType == messageItemType);
            if (exactMatch != null)
            {
                return exactMatch;
            }

            // 2. LineNumberが一致するもの
            var lineMatch = Items.FirstOrDefault(x => x.LineNumber == messageLineNumber);
            if (lineMatch != null)
            {
                return lineMatch;
            }

            // 3. ItemTypeが一致し、LineNumberが近いもの（±2の範囲）
            var typeAndNearLineMatch = Items.FirstOrDefault(x => 
                x.ItemType == messageItemType && 
                Math.Abs(x.LineNumber - messageLineNumber) <= 2);
            if (typeAndNearLineMatch != null)
            {
                return typeAndNearLineMatch;
            }

            // 4. 実行中のアイテムがあればそれを優先
            var runningItem = Items.FirstOrDefault(x => x.IsRunning);
            if (runningItem != null && runningItem.ItemType == messageItemType)
            {
                return runningItem;
            }

            return null;
        }

        #endregion

        #region 不足していたメソッドの実装

        private void ChangeItemType(ICommandListItem oldItem, ICommandListItem newItem)
        {
            try
            {
                var index = Items.IndexOf(oldItem);
                if (index >= 0)
                {
                    var operation = new CommandListOperation
                    {
                        Type = OperationType.Replace,
                        Index = index,
                        Item = oldItem.Clone(),
                        NewItem = newItem.Clone(),
                        Description = $"タイプ変更: {oldItem.ItemType} -> {newItem.ItemType}"
                    };

                    Items[index] = newItem;
                    SelectedIndex = index;
                    SelectedItem = newItem;

                    UpdateLineNumbers();
                    RecordOperation(operation);
                    StatusMessage = $"タイプを変更しました: {oldItem.ItemType} -> {newItem.ItemType}";
                    
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムタイプ変更中にエラーが発生しました");
                StatusMessage = $"タイプ変更エラー: {ex.Message}";
            }
        }

        private void RefreshList()
        {
            try
            {
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));

                StatusMessage = "リストを更新しました";
                _logger.LogDebug("リストビューを強制更新しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "リスト更新中にエラーが発生しました");
                StatusMessage = $"更新エラー: {ex.Message}";
            }
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            StatusMessage = isRunning ? "実行中..." : "準備完了";
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
            
            if (!isRunning)
            {
                if (ShowProgress)
                {
                    CompleteProgress();
                }
                
                foreach (var item in Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }
                CurrentExecutingItem = null;
            }
        }

        public void TestExecutionStateDisplay()
        {
            try
            {
                _logger.LogInformation("=== 実行状態表示テスト開始 ===");
                
                if (Items.Count == 0)
                {
                    _logger.LogWarning("テスト対象のアイテムがありません");
                    return;
                }

                var testItem = Items.First();
                _logger.LogInformation("テスト対象アイテム: {ItemType} (行{LineNumber})", testItem.ItemType, testItem.LineNumber);

                testItem.IsRunning = true;
                testItem.Progress = 50;
                CurrentExecutingItem = testItem;

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(CurrentExecutingItem));
                OnPropertyChanged(nameof(CurrentExecutingDescription));
                
                _logger.LogInformation("=== 実行状態表示テスト完了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行状態表示テスト中にエラー");
            }
        }

        public void DebugItemStates()
        {
            try
            {
                _logger.LogInformation("=== アイテム状態一覧 ===");
                _logger.LogInformation("総アイテム数: {Count}", Items.Count);
                _logger.LogInformation("CurrentExecutingItem: {Item}", CurrentExecutingItem?.ItemType ?? "null");

                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    _logger.LogInformation("  [{Index}] {ItemType} (行{LineNumber}) - IsRunning:{IsRunning}, Progress:{Progress}%", 
                        i, item.ItemType, item.LineNumber, item.IsRunning, item.Progress);
                }
                _logger.LogInformation("=== アイテム状態一覧完了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム状態表示中にエラー");
            }
        }

        #endregion
    }

    #region 補助クラス

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

    public enum OperationType
    {
        Add,
        Delete,
        Move,
        Clear,
        Edit,
        Replace
    }

    public class CommandListStats
    {
        public int TotalItems { get; set; }
        public int EnabledItems { get; set; }
        public int DisabledItems { get; set; }
        public Dictionary<string, int> ItemTypeStats { get; set; } = new();
    }

    #endregion
}