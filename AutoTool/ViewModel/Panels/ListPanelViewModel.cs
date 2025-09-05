using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using AutoTool.List.Class;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// DI対応版：ListPanelViewModel（CommandListService統合）
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandListService _commandListService;
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

                OnPropertyChanged(nameof(CurrentExecutingDescription));
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

        public double ProgressPercentage => TotalProgress > 0 ? (double)CurrentProgress / TotalProgress * 100 : 0;

        public string EstimatedTimeRemaining
        {
            get
            {
                if (TotalProgress <= 0 || CurrentProgress <= 0) return "不明";

                var remaining = TotalProgress - CurrentProgress;
                if (remaining <= 0) return "完了";

                var estimatedSeconds = remaining * 2;
                return $"約{estimatedSeconds}秒";
            }
        }

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

        public ListPanelViewModel(
            ILogger<ListPanelViewModel> logger,
            IServiceProvider serviceProvider,
            CommandListService commandListService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _commandListService = commandListService ?? throw new ArgumentNullException(nameof(commandListService));

            SetupMessaging();
            _logger.LogInformation("DI対応ListPanelViewModel を初期化しています（CommandListService統合版）");

            _items.CollectionChanged += (s, e) =>
            {
                TotalItems = _items.Count;
                HasUnsavedChanges = true;

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));

                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(_items.Count));

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
            WeakReferenceMessenger.Default.Register<AddUniversalItemMessage>(this, (r, m) => AddUniversalItem(m.Item));
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) => DeleteInternal());
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) => MoveUpInternal());
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) => MoveDownInternal());
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) => ClearInternal());
            WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) => UndoInternal());
            WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) => RedoInternal());

            // コマンド実行状態メッセージの処理
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) => OnCommandStarted(m));
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) => OnCommandFinished(m));
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (r, m) => OnProgressUpdated(m));
            WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, (r, m) => OnCommandDoing(m));

            // ファイル操作メッセージの処理（両方のメッセージタイプに対応）
            WeakReferenceMessenger.Default.Register<LoadMessage>(this, (r, m) => LoadFileInternal(m.FilePath ?? string.Empty));
            WeakReferenceMessenger.Default.Register<SaveMessage>(this, (r, m) => SaveFileInternal(m.FilePath ?? string.Empty));
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

        public void AddUniversalItem(UniversalCommandItem universalItem)
        {
            try
            {
                _logger.LogDebug("動的UniversalCommandItemを追加します: {ItemType}", universalItem.ItemType);

                // UniversalCommandItemからICommandListItemに変換
                var newItem = ConvertUniversalItemToCommandListItem(universalItem);

                var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;

                var operation = new CommandListOperation
                {
                    Type = OperationType.Add,
                    Index = insertIndex,
                    Item = newItem.Clone(),
                    Description = $"動的アイテム追加: {universalItem.ItemType}"
                };

                Items.Insert(insertIndex, newItem);

                SelectedIndex = insertIndex;
                SelectedItem = newItem;

                RecordOperation(operation);
                StatusMessage = $"動的{universalItem.ItemType}を追加しました";

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));

                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));

                _logger.LogInformation("動的アイテムを追加しました: {ItemType} (合計 {Count}件)", universalItem.ItemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的アイテム追加中にエラーが発生しました");
                StatusMessage = $"動的追加エラー: {ex.Message}";
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

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));

                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
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

        #region ファイル操作

        private void LoadFileInternal(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("ファイル読み込み: パスが空です");
                    return;
                }

                _logger.LogInformation("メッセージからのファイル読み込み開始: {FilePath}", filePath);
                Load(filePath);
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                WeakReferenceMessenger.Default.Send(new LogMessage("Info", $"ファイル読み込み完了: {Path.GetFileName(filePath)} ({Items.Count}件)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラー: {FilePath}", filePath);
                WeakReferenceMessenger.Default.Send(new LogMessage("Error", $"ファイル読み込み失敗: {ex.Message}"));
            }
        }

        private void SaveFileInternal(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("ファイル保存: パスが空です");
                    return;
                }

                _logger.LogInformation("メッセージからのファイル保存開始: {FilePath}", filePath);
                Save(filePath);
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
                _logger.LogInformation("=== ListPanelViewModel.Load開始 ===");
                _logger.LogInformation("対象ファイル: {FilePath}", filePath);

                if (System.IO.File.Exists(filePath))
                {
                    _logger.LogInformation("ファイル存在確認OK、CommandListServiceを使用してファイル読み込み開始");

                    BaseCommand.SetMacroFileBasePath(filePath);

                    _commandListService.Load(filePath);
                    _logger.LogInformation("CommandListServiceからの読み込み成功: {Count}個", _commandListService.Items.Count);

                    Items.Clear();

                    foreach (var item in _commandListService.Items)
                    {
                        Items.Add(item);
                    }

                    SelectedIndex = Items.Count > 0 ? 0 : -1;
                    SelectedItem = Items.FirstOrDefault();

                    HasUnsavedChanges = false;
                    StatusMessage = $"ファイルを読み込みました: {Path.GetFileName(filePath)} ({Items.Count}個)";
                    _logger.LogInformation("=== ListPanelViewModel.Load完了: {FilePath} ({Count}個) ===", filePath, Items.Count);
                }
                else
                {
                    _logger.LogWarning("ファイルが存在しません: {FilePath}", filePath);
                    Items.Clear();
                    BaseCommand.SetMacroFileBasePath(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanelViewModel.Load中にエラーが発生しました: {FilePath}", filePath);
                StatusMessage = $"読み込みエラー: {ex.Message}";
                BaseCommand.SetMacroFileBasePath(null);
                throw;
            }
        }

        public void Save(string filePath)
        {
            try
            {
                StatusMessage = "ファイル保存中...";

                _logger.LogInformation("CommandListServiceを使用してファイル保存開始: {FilePath}", filePath);

                _commandListService.Items.Clear();
                foreach (var item in Items)
                {
                    _commandListService.Items.Add(item);
                }

                _commandListService.Save(filePath);
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

        #region ヘルパーメソッド

        private ICommandListItem CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("新しいアイテム作成開始: {TypeName}", itemType);

                // CommandRegistryを使用してアイテムを作成
                var newItem = CommandRegistry.CreateCommandItem(itemType);
                if (newItem != null)
                {
                    newItem.LineNumber = GetNextLineNumber();
                    newItem.Comment = $"{CommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";

                    _logger.LogInformation("CommandRegistryでコマンドアイテムを作成: {ItemType}", itemType);
                    return newItem;
                }

                // 最後の手段：Activatorで直接作成を試みる
                var typeFullName = $"AutoTool.Model.List.Class.{itemType}Item";
                var targetType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeFullName);

                if (targetType != null)
                {
                    if (Activator.CreateInstance(targetType) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.LineNumber = Items.Count + 1;
                        fallbackItem.Comment = $"{CommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";

                        _logger.LogInformation("Activatorでコマンドアイテムを作成: {ItemType}", itemType);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("コマンドアイテムの作成に失敗: {TypeName}", itemType);

                // フォールバック: 基本的なCommandListItem
                return new CommandListItem
                {
                    ItemType = itemType,
                    LineNumber = GetNextLineNumber(),
                    IsEnable = true,
                    Comment = $"{itemType}コマンド"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateItem中にエラーが発生: {ItemType}", itemType);

                return new CommandListItem
                {
                    ItemType = itemType,
                    LineNumber = Items.Count + 1,
                    IsEnable = true,
                    Comment = $"{itemType}コマンド（エラー復旧）"
                };
            }
        }

        private void UpdateLineNumbers()
        {
            try
            {
                if (_items.Count == 0) return;

                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].LineNumber = i + 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "行番号更新中にエラーが発生しました: {Message}", ex.Message);
            }
        }

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

        /// <summary>
        /// UniversalCommandItemをICommandListItemに変換
        /// </summary>
        private ICommandListItem ConvertUniversalItemToCommandListItem(UniversalCommandItem universalItem)
        {
            try
            {
                // UniversalCommandItemはICommandListItemを実装しているかチェック
                if (universalItem is ICommandListItem commandListItem)
                {
                    commandListItem.LineNumber = Items.Count + 1;
                    _logger.LogDebug("UniversalCommandItem を ICommandListItem として使用: {ItemType}", universalItem.ItemType);
                    return commandListItem;
                }

                // UniversalCommandItemWrapper を作成
                var wrapper = new UniversalCommandItemWrapper(universalItem)
                {
                    LineNumber = Items.Count + 1
                };

                _logger.LogDebug("UniversalCommandItemWrapper を作成: {ItemType}", universalItem.ItemType);
                return wrapper;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UniversalCommandItem変換中にエラー: {ItemType}", universalItem.ItemType);

                // フォールバック: BasicCommandItem
                return new BasicCommandItem
                {
                    ItemType = universalItem.ItemType,
                    LineNumber = Items.Count + 1,
                    IsEnable = universalItem.IsEnable,
                    Comment = $"動的{universalItem.ItemType}コマンド"
                };
            }
        }

        #endregion

        #region コマンド実行状態管理

        // 最後に実行されたアイテムを追跡
        private ICommandListItem? _lastExecutedItem;
        private readonly Dictionary<string, int> _executionCounter = new();

        private void OnCommandStarted(StartCommandMessage message)
        {
            try
            {
                _logger.LogDebug("=== OnCommandStarted開始 ===");
                _logger.LogDebug("メッセージ受信: Line={LineNumber}, Type={ItemType}", message.LineNumber, message.ItemType);

                // 実行カウンターを更新
                var key = $"{message.ItemType}";
                _executionCounter[key] = _executionCounter.GetValueOrDefault(key, 0) + 1;
                _logger.LogDebug("実行カウンター更新: {ItemType} = {Count}", message.ItemType, _executionCounter[key]);

                // より柔軟な検索ロジック
                var item = FindMatchingItem(message.LineNumber, message.ItemType);

                if (item != null)
                {
                    _logger.LogDebug("アイテム発見: Line={LineNumber}, Type={ItemType}, IsEnable={IsEnable}, IsRunning={IsRunning}",
                        item.LineNumber, item.ItemType, item.IsEnable, item.IsRunning);

                    item.IsRunning = true;
                    item.Progress = 0;
                    CurrentExecutingItem = item;
                    _lastExecutedItem = item;

                    _logger.LogDebug("コマンド開始: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})",
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);

                    // UIに変更を通知
                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(CurrentExecutingItem));
                    OnPropertyChanged(nameof(CurrentExecutingDescription));

                    _logger.LogDebug("UI更新完了: CurrentExecutingDescription={Description}", CurrentExecutingDescription);
                }
                else
                {
                    _logger.LogWarning("コマンド開始: 対応するアイテムが見つかりません - Line {MessageLineNumber}, Type {MessageItemType}",
                        message.LineNumber, message.ItemType);

                    // 詳細なデバッグ情報を出力
                    _logger.LogDebug("現在のアイテム一覧 (総数: {Count}):", Items.Count);
                    _logger.LogDebug("実行カウンター状況:");
                    foreach (var kvp in _executionCounter)
                    {
                        _logger.LogDebug("  {ItemType}: {Count}回", kvp.Key, kvp.Value);
                    }

                    foreach (var debugItem in Items.Take(15))
                    {
                        _logger.LogDebug("  Line {LineNumber}: {ItemType} (IsEnable: {IsEnable}, IsRunning: {IsRunning}, Progress: {Progress})",
                            debugItem.LineNumber, debugItem.ItemType, debugItem.IsEnable, debugItem.IsRunning, debugItem.Progress);
                    }
                }

                _logger.LogDebug("=== OnCommandStarted終了 ===");
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
                _logger.LogDebug("=== OnCommandFinished開始 ===");
                _logger.LogDebug("メッセージ受信: Line={LineNumber}, Type={ItemType}", message.LineNumber, message.ItemType);

                var item = FindMatchingItem(message.LineNumber, message.ItemType);

                if (item != null)
                {
                    _logger.LogDebug("アイテム発見: Line={LineNumber}, Type={ItemType}, IsRunning={IsRunning}",
                        item.LineNumber, item.ItemType, item.IsRunning);

                    item.IsRunning = false;
                    item.Progress = 100;

                    if (item.IsEnable)
                    {
                        _completedCommands++;
                        UpdateProgress(_completedCommands);
                        _logger.LogDebug("進捗更新: 完了コマンド数={CompletedCommands}", _completedCommands);
                    }

                    _logger.LogDebug("コマンド完了: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})",
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);

                    // CurrentExecutingItemをクリア
                    if (CurrentExecutingItem == item)
                    {
                        CurrentExecutingItem = null;
                        _logger.LogDebug("CurrentExecutingItemをクリア");
                    }

                    // UIに変更を通知
                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(CurrentExecutingItem));
                    OnPropertyChanged(nameof(CurrentExecutingDescription));

                    _logger.LogDebug("UI更新完了: CurrentExecutingDescription={Description}", CurrentExecutingDescription);

                    // 一定時間後にプログレスをリセット
                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        if (!item.IsRunning)
                        {
                            item.Progress = 0;
                            OnPropertyChanged(nameof(Items));
                            _logger.LogTrace("プログレスリセット完了: Line={LineNumber}", item.LineNumber);
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("コマンド完了: 対応するアイテムが見つかりません - Line {MessageLineNumber}, Type {MessageItemType}",
                        message.LineNumber, message.ItemType);
                }

                _logger.LogDebug("=== OnCommandFinished終了 ===");
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
                        item.LineNumber, message.LineNumber, item.Progress, item.ItemType, message.ItemType);

                    // UIに変更を通知
                    OnPropertyChanged(nameof(Items));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "進捗更新処理中にエラーが発生しました");
            }
        }

        private void OnCommandDoing(DoingCommandMessage message)
        {
            try
            {
                _logger.LogDebug("=== OnCommandDoing開始 ===");
                _logger.LogDebug("メッセージ受信: Line={LineNumber}, Type={ItemType}, Detail={Detail}",
                    message.LineNumber, message.ItemType, message.Detail);

                var item = FindMatchingItem(message.LineNumber, message.ItemType);

                if (item != null)
                {
                    _logger.LogDebug("アイテム発見: Line={LineNumber}, Type={ItemType}, IsRunning={IsRunning}",
                        item.LineNumber, item.ItemType, item.IsRunning);

                    // コマンドが実行中であることを確認
                    if (!item.IsRunning)
                    {
                        _logger.LogDebug("実行状態でないアイテムを実行中に設定: Line={LineNumber}, Type={ItemType}",
                            item.LineNumber, item.ItemType);

                        item.IsRunning = true;
                        item.Progress = 0;
                        CurrentExecutingItem = item;

                        _logger.LogDebug("DoingMessage受信時にコマンド実行状態を設定: Line {LineNumber} - {ItemType}",
                            item.LineNumber, item.ItemType);
                    }

                    // UIに変更を通知
                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(CurrentExecutingItem));
                    OnPropertyChanged(nameof(CurrentExecutingDescription));

                    _logger.LogDebug("UI更新完了: CurrentExecutingDescription={Description}", CurrentExecutingDescription);

                    _logger.LogTrace("コマンド実行中: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType}) - {Detail}",
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType, message.Detail);
                }
                else
                {
                    _logger.LogWarning("DoingMessage: 対応するアイテムが見つかりません - Line {MessageLineNumber}, Type {MessageItemType}",
                        message.LineNumber, message.ItemType);

                    // 詳細なデバッグ情報を出力
                    _logger.LogDebug("DoingMessage - 現在のアイテム一覧 (総数: {Count}):", Items.Count);
                    foreach (var debugItem in Items.Take(10))
                    {
                        _logger.LogDebug("  Line {LineNumber}: {ItemType} (IsEnable: {IsEnable}, IsRunning: {IsRunning})",
                            debugItem.LineNumber, debugItem.ItemType, debugItem.IsEnable, debugItem.IsRunning);
                    }
                }

                _logger.LogDebug("=== OnCommandDoing終了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoingMessage処理中にエラーが発生しました");
            }
        }

        private ICommandListItem? FindMatchingItem(int messageLineNumber, string messageItemType)
        {
            try
            {
                var cleanMessageType = CleanItemType(messageItemType);

                _logger.LogTrace("=== FindMatchingItem開始 ===");
                _logger.LogTrace("検索条件: MessageLine={MessageLine}, MessageType={MessageType}, CleanType={CleanType}",
                    messageLineNumber, messageItemType, cleanMessageType);

                // 修正: LineNumber=0の場合のみ実行順序ベースの検索を行う
                // LineNumber > 0の場合は通常の検索を優先

                // 1. 正確な一致を最優先（LineNumber + ItemType）
                var exactMatch = Items.FirstOrDefault(x =>
                    x.LineNumber == messageLineNumber &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType));

                if (exactMatch != null)
                {
                    _logger.LogTrace("FindMatchingItem: 正確な一致発見 - Line:{Line}, Type:{Type}",
                        exactMatch.LineNumber, exactMatch.ItemType);
                    return exactMatch;
                }

                // 2. LineNumberが一致するもの（複数ある場合は最初の有効なもの）
                var sameLineItems = Items.Where(x => x.LineNumber == messageLineNumber && x.IsEnable).ToList();
                _logger.LogTrace("同一行の有効アイテム数: {Count}", sameLineItems.Count);

                if (sameLineItems.Count == 1)
                {
                    _logger.LogTrace("FindMatchingItem: 同一行の有効アイテム発見 - Line:{Line}, Type:{Type}",
                        sameLineItems[0].LineNumber, sameLineItems[0].ItemType);
                    return sameLineItems[0];
                }
                else if (sameLineItems.Count > 1)
                {
                    // 複数ある場合はタイプが類似しているものを優先
                    var similarTypeMatch = sameLineItems.FirstOrDefault(x =>
                        AreItemTypesSimilar(x.ItemType, messageItemType));
                    if (similarTypeMatch != null)
                    {
                        _logger.LogTrace("FindMatchingItem: 同一行の類似タイプ発見 - Line:{Line}, Type:{Type}",
                            similarTypeMatch.LineNumber, similarTypeMatch.ItemType);
                        return similarTypeMatch;
                    }

                    // 類似タイプがない場合は最初のもの
                    _logger.LogTrace("FindMatchingItem: 同一行の最初のアイテム選択 - Line:{Line}, Type:{Type}",
                        sameLineItems[0].LineNumber, sameLineItems[0].ItemType);
                    return sameLineItems[0];
                }

                // 3. LineNumber=0の場合のみ実行順序ベースの検索を使用
                if (messageLineNumber == 0)
                {
                    _logger.LogTrace("LineNumber=0のため実行順序ベース検索を実行");
                    return FindItemByExecutionOrder(messageItemType, cleanMessageType);
                }

                // 4. 近い行番号でタイプが一致するもの（±3の範囲）
                var nearbyMatch = Items.FirstOrDefault(x =>
                    Math.Abs(x.LineNumber - messageLineNumber) <= 3 &&
                    x.IsEnable &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                     AreItemTypesSimilar(x.ItemType, messageItemType)));

                if (nearbyMatch != null)
                {
                    _logger.LogTrace("FindMatchingItem: 近隣行でタイプ一致 - Line:{Line}, Type:{Type} (距離:{Distance})",
                        nearbyMatch.LineNumber, nearbyMatch.ItemType, Math.Abs(nearbyMatch.LineNumber - messageLineNumber));
                    return nearbyMatch;
                }

                _logger.LogWarning("FindMatchingItem: 一致するアイテムが見つかりません - MessageLine:{MessageLine}, MessageType:{MessageType}",
                    messageLineNumber, messageItemType);

                _logger.LogTrace("=== FindMatchingItem終了: null ===");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindMatchingItem中にエラー");
                return null;
            }
        }

        /// <summary>
        /// LineNumber=0の場合の実行順序ベースの検索
        /// </summary>
        private ICommandListItem? FindItemByExecutionOrder(string messageItemType, string cleanMessageType)
        {
            try
            {
                _logger.LogTrace("実行順序ベース検索開始: MessageType={MessageType}, CleanType={CleanType}",
                    messageItemType, cleanMessageType);

                // 1. 現在実行中のアイテムからタイプ一致を検索
                var runningItems = Items.Where(x => x.IsRunning).ToList();
                _logger.LogTrace("実行中のアイテム数: {Count}", runningItems.Count);

                foreach (var runningItem in runningItems)
                {
                    if (runningItem.ItemType == messageItemType ||
                        runningItem.ItemType == cleanMessageType ||
                        AreItemTypesSimilar(runningItem.ItemType, messageItemType))
                    {
                        _logger.LogTrace("FindItemByExecutionOrder: 実行中アイテムからタイプ一致 - Line:{Line}, Type:{Type}",
                            runningItem.LineNumber, runningItem.ItemType);
                        return runningItem;
                    }
                }

                // 2. 実行可能な次のアイテムを検索（順序考慮）
                var nextExecutableItem = FindNextExecutableItem(messageItemType, cleanMessageType);
                if (nextExecutableItem != null)
                {
                    _logger.LogTrace("FindItemByExecutionOrder: 次の実行可能アイテム発見 - Line:{Line}, Type:{Type}",
                        nextExecutableItem.LineNumber, nextExecutableItem.ItemType);
                    return nextExecutableItem;
                }

                // 3. タイプのみで一致（最初に見つかったもの）
                var typeOnlyMatch = Items.FirstOrDefault(x =>
                    x.IsEnable &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                     AreItemTypesSimilar(x.ItemType, messageItemType)));


                if (typeOnlyMatch != null)
                {
                    _logger.LogTrace("FindItemByExecutionOrder: タイプのみ一致 - Line:{Line}, Type:{Type}",
                        typeOnlyMatch.LineNumber, typeOnlyMatch.ItemType);
                    return typeOnlyMatch;
                }

                _logger.LogWarning("FindItemByExecutionOrder: 実行順序ベース検索で一致するアイテムが見つかりません");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindItemByExecutionOrder中にエラー");
                return null;
            }
        }

        /// <summary>
        /// 実行順序を考慮して次に実行されるべきアイテムを検索
        /// </summary>
        private ICommandListItem? FindNextExecutableItem(string messageItemType, string cleanMessageType)
        {
            try
            {
                // 最後に実行されたアイテムを取得
                var lastExecutedItem = CurrentExecutingItem ??
                    Items.Where(x => x.Progress > 0).OrderByDescending(x => x.Progress).FirstOrDefault() ??
                    Items.FirstOrDefault(x => x.IsRunning);

                if (lastExecutedItem != null)
                {
                    _logger.LogTrace("最後の実行アイテム: Line={LineNumber}, Type={ItemType}",
                        lastExecutedItem.LineNumber, lastExecutedItem.ItemType);

                    // 次に実行されるべきアイテムを論理的順序で検索
                    var candidateItems = Items.Where(x =>
                        x.IsEnable &&
                        x.LineNumber > lastExecutedItem.LineNumber &&
                        (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                         AreItemTypesSimilar(x.ItemType, messageItemType))).ToList();

                    if (candidateItems.Count > 0)
                    {
                        var nextItem = candidateItems.OrderBy(x => x.LineNumber).First();
                        _logger.LogTrace("論理的次のアイテム発見: Line={LineNumber}, Type={ItemType}",
                            nextItem.LineNumber, nextItem.ItemType);
                        return nextItem;
                    }
                }

                // フォールバック: 最初の未実行のマッチするアイテム
                var firstMatch = Items.Where(x =>
                    x.IsEnable &&
                    !x.IsRunning &&
                    x.Progress == 0 &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                     AreItemTypesSimilar(x.ItemType, messageItemType))).OrderBy(x => x.LineNumber).FirstOrDefault();

                if (firstMatch != null)
                {
                    _logger.LogTrace("最初の未実行アイテム発見: Line={LineNumber}, Type={ItemType}",
                        firstMatch.LineNumber, firstMatch.ItemType);
                }

                return firstMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindNextExecutableItem中にエラー");
                return null;
            }
        }

        private int GetNextLineNumber()
        {
            return Items.Count > 0 ? Items.Max(i => i.LineNumber) + 1 : 1;
        }

        private string CleanItemType(string itemType)
        {
            if (string.IsNullOrEmpty(itemType)) return itemType;

            if (itemType.EndsWith("Command"))
            {
                return itemType.Substring(0, itemType.Length - "Command".Length);
            }

            return itemType;
        }

        private bool AreItemTypesSimilar(string type1, string type2)
        {
            if (string.IsNullOrEmpty(type1) || string.IsNullOrEmpty(type2)) return false;

            var clean1 = CleanItemType(type1);
            var clean2 = CleanItemType(type2);

            if (clean1.Equals(clean2, StringComparison.OrdinalIgnoreCase)) return true;

            return (clean1, clean2) switch
            {
                ("WaitImage", "Wait_Image") or ("Wait_Image", "WaitImage") => true,
                ("ClickImage", "Click_Image") or ("Click_Image", "ClickImage") => true,
                ("ClickImageAI", "Click_Image_AI") or ("Click_Image_AI", "ClickImageAI") => true,
                ("IfImageExist", "IF_ImageExist") or ("IF_ImageExist", "IfImageExist") => true,
                ("IfImageNotExist", "IF_ImageNotExist") or ("IF_ImageNotExist", "IfImageNotExist") => true,
                ("IfImageExistAI", "IF_ImageExist_AI") or ("IF_ImageExist_AI", "IfImageExistAI") => true,
                ("IfImageNotExistAI", "IF_ImageNotExist_AI") or ("IF_ImageNotExist_AI", "IfImageNotExistAI") => true,
                ("SetVariableAI", "SetVariable_AI") or ("SetVariable_AI", "SetVariableAI") => true,
                ("IfVariable", "IF_Variable") or ("IF_Variable", "IfVariable") => true,
                ("Wait", "Wait") => true, // Waitコマンドの一致条件を追加
                _ => false
            };
        }

        public void SetRunningState(bool isRunning)
        {
            _logger.LogDebug("=== SetRunningState開始: {IsRunning} ===", isRunning);

            IsRunning = isRunning;
            StatusMessage = isRunning ? "実行中..." : "準備完了";
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);

            if (isRunning)
            {
                _logger.LogDebug("実行開始 - プログレス初期化");
                InitializeProgress();
                _completedCommands = 0;

                // 実行カウンターをリセット
                _executionCounter.Clear();
                _lastExecutedItem = null;
                _logger.LogDebug("実行カウンターと追跡状態をリセット");

                // 全アイテムの実行状態をリセット
                foreach (var item in Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }

                _logger.LogDebug("実行開始 - 全アイテム状態リセット完了");
            }
            else
            {
                _logger.LogDebug("実行終了 - クリーンアップ開始");

                if (ShowProgress)
                {
                    CompleteProgress();
                }

                // 実行カウンターと追跡状態をクリア
                _executionCounter.Clear();
                _lastExecutedItem = null;

                // 全アイテムの実行状態をクリア
                var runningCount = 0;
                foreach (var item in Items)
                {
                    if (item.IsRunning)
                    {
                        runningCount++;
                        item.IsRunning = false;
                        item.Progress = 0;
                    }
                }

                CurrentExecutingItem = null;

                _logger.LogDebug("実行終了 - {RunningCount}個のアイテムの実行状態をクリア", runningCount);

                // UIに変更を通知
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(CurrentExecutingItem));
                OnPropertyChanged(nameof(CurrentExecutingDescription));
            }

            _logger.LogDebug("=== SetRunningState終了: {IsRunning} ===", isRunning);
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

    #endregion
}