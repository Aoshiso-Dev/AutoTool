using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using AutoTool.Command.Interface;
using AutoTool.Services;
using AutoTool.Services.Execution;
using AutoTool.Services.MacroFile;
using AutoTool.Message;
using AutoTool.ViewModel.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AutoTool.Command.Definition;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// ListPanelViewModel (Phase 5統合版) - CommandRegistry -> DirectCommandRegistry対応
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject, IRecipient<RunMessage>, IRecipient<StopMessage>, IRecipient<AddMessage>, IRecipient<ClearMessage>, IRecipient<UpMessage>, IRecipient<DownMessage>, IRecipient<DeleteMessage>, IRecipient<UndoMessage>, IRecipient<RedoMessage>, IRecipient<LoadMessage>, IRecipient<SaveMessage>, IDisposable
    {
        // 静的な重複実行防止フラグ（全インスタンス共有）
        private static volatile bool _globalExecutionInProgress = false;
        private static readonly object _globalExecutionLock = new object();
        
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUniversalCommandItemFactory _commandListItemFactory;
        private readonly RecentFileService _recentFileService;
        private readonly IMacroExecutionService _macroExecutionService;
        private readonly IMacroFileService _macroFileService;

        private ICommand? _currentExecutingCommand;
        private CancellationTokenSource? _cancellationTokenSource;
        
        // 重複実行防止用の変数
        private readonly object _runMessageLock = new object();
        private bool _isProcessingRunMessage = false;
        private readonly SemaphoreSlim _executionSemaphore = new SemaphoreSlim(1, 1);

        [ObservableProperty]
        private ObservableCollection<UniversalCommandItem> _items = new();

        [ObservableProperty]
        private UniversalCommandItem? _selectedItem;

        [ObservableProperty]
        private int _selectedIndex = -1;

        [ObservableProperty]
        private UniversalCommandItem? _currentExecutingItem;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _canExecute = true;

        [ObservableProperty]
        private int _totalItems;

        [ObservableProperty]
        private int _executedItems;

        [ObservableProperty]
        private int _currentLineNumber;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private ObservableCollection<string> _executionLog = new();

        [ObservableProperty]
        private bool _isLogVisible = false;

        [ObservableProperty]
        private string _currentFileName = "新規ファイル";

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        [ObservableProperty]
        private ObservableCollection<string> _undoStack = new();

        [ObservableProperty]
        private ObservableCollection<string> _redoStack = new();

        [ObservableProperty]
        private TimeSpan _elapsedTime;

        [ObservableProperty]
        private string _currentCommandDescription = string.Empty;

        /// <summary>
        /// アイテムが存在するかどうか
        /// </summary>
        public bool HasItems => Items.Count > 0;

        /// <summary>
        /// Undoが可能かどうか
        /// </summary>
        public bool CanUndo => UndoStack.Count > 0;

        /// <summary>
        /// Redoが可能かどうか
        /// </summary>
        public bool CanRedo => RedoStack.Count > 0;

        public ListPanelViewModel(
            ILogger<ListPanelViewModel> logger,
            IServiceProvider serviceProvider,
            IUniversalCommandItemFactory commandListItemFactory,
            RecentFileService recentFileService,
            IMacroExecutionService macroExecutionService,
            IMacroFileService macroFileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _commandListItemFactory = commandListItemFactory ?? throw new ArgumentNullException(nameof(commandListItemFactory));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _macroExecutionService = macroExecutionService ?? throw new ArgumentNullException(nameof(macroExecutionService));
            _macroFileService = macroFileService ?? throw new ArgumentNullException(nameof(macroFileService));

            TotalItems = Items.Count;

            // MacroExecutionServiceのイベント購読
            _macroExecutionService.StatusChanged += OnMacroExecutionStatusChanged;
            _macroExecutionService.RunningStateChanged += OnMacroExecutionRunningStateChanged;
            _macroExecutionService.CommandCountChanged += OnMacroExecutionCommandCountChanged;
            _macroExecutionService.ExecutedCountChanged += OnMacroExecutionExecutedCountChanged;
            _macroExecutionService.ProgressChanged += OnMacroExecutionProgressChanged;

            // メッセージングシステムに登録（重複登録防止）
            WeakReferenceMessenger.Default.UnregisterAll(this); // 既存の登録をクリア
            
            WeakReferenceMessenger.Default.Register<RunMessage>(this);
            WeakReferenceMessenger.Default.Register<StopMessage>(this);
            WeakReferenceMessenger.Default.Register<AddMessage>(this);
            WeakReferenceMessenger.Default.Register<ClearMessage>(this);
            WeakReferenceMessenger.Default.Register<UpMessage>(this);
            WeakReferenceMessenger.Default.Register<DownMessage>(this);
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this);
            WeakReferenceMessenger.Default.Register<UndoMessage>(this);
            WeakReferenceMessenger.Default.Register<RedoMessage>(this);
            WeakReferenceMessenger.Default.Register<LoadMessage>(this);
            WeakReferenceMessenger.Default.Register<SaveMessage>(this);

            // プログレス表示とハイライト用のメッセージ登録
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, OnStartCommandMessage);
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, OnFinishCommandMessage);
            WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, OnDoingCommandMessage);
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, OnUpdateProgressMessage);
            WeakReferenceMessenger.Default.Register<CommandErrorMessage>(this, OnCommandErrorMessage);

            _logger.LogInformation("ListPanelViewModel (Phase 5) が初期化されました");

            // プロパティ変更の監視
            Items.CollectionChanged += (s, e) =>
            {
                TotalItems = Items.Count;
                HasUnsavedChanges = true;
                OnPropertyChanged(nameof(HasItems));

                // CanExecuteを更新
                CanExecute = !IsRunning && Items.Count > 0;
                OnPropertyChanged(nameof(CanExecute));
            };
        }

        public void Receive(RunMessage message)
        {
            // 最初のグローバルチェック
            lock (_globalExecutionLock)
            {
                if (_globalExecutionInProgress)
                {
                    _logger.LogWarning("グローバル実行中のためRunMessage受信をブロック");
                    return;
                }
            }

            // RunMessage受信の排他制御
            lock (_runMessageLock)
            {
                if (_isProcessingRunMessage)
                {
                    _logger.LogWarning("RunMessage重複受信をブロック: 既に処理中です");
                    return;
                }
                _isProcessingRunMessage = true;
            }

            try
            {
                _logger.LogInformation("RunMessage受信: Items.Count={ItemCount}, IsRunning={IsRunning}, CanExecute={CanExecute}, MacroServiceRunning={MacroServiceRunning}",
                    Items.Count, IsRunning, CanExecute, _macroExecutionService.IsRunning);

                if (Items.Count == 0)
                {
                    StatusMessage = "実行するコマンドがありません";
                    _logger.LogWarning("RunMessage処理スキップ: コマンドリストが空です");
                    return;
                }

                if (IsRunning || _macroExecutionService.IsRunning)
                {
                    StatusMessage = "マクロは既に実行中です";
                    _logger.LogWarning("RunMessage処理スキップ: マクロは既に実行中です (ListPanel={ListPanelRunning}, MacroService={MacroServiceRunning})", 
                        IsRunning, _macroExecutionService.IsRunning);
                    return;
                }

                // グローバル実行フラグを設定
                lock (_globalExecutionLock)
                {
                    if (_globalExecutionInProgress)
                    {
                        _logger.LogWarning("グローバル実行開始直前でブロック");
                        return;
                    }
                    _globalExecutionInProgress = true;
                }

                _logger.LogInformation("マクロ実行開始（グローバル制御開始）");
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await ExecuteMacroAsync();
                    }
                    finally
                    {
                        // RunMessage処理完了をマーク
                        lock (_runMessageLock)
                        {
                            _isProcessingRunMessage = false;
                        }
                        
                        // グローバル実行フラグをリセット
                        lock (_globalExecutionLock)
                        {
                            _globalExecutionInProgress = false;
                            _logger.LogDebug("グローバル実行フラグをリセットしました");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMessage処理中にエラー");
                lock (_runMessageLock)
                {
                    _isProcessingRunMessage = false;
                }
                lock (_globalExecutionLock)
                {
                    _globalExecutionInProgress = false;
                }
            }
        }

        public void Receive(StopMessage message)
        {
            _logger.LogInformation("マクロ実行停止要求");
            StopMacro();
        }

        public void Receive(AddMessage message)
        {
            _logger.LogDebug("コマンド追加要求: {ItemType}", message.ItemType);
            AddItem(message.ItemType);
        }

        public void Receive(ClearMessage message)
        {
            _logger.LogDebug("リストクリア要求");
            ClearAll();
        }

        public void Receive(UpMessage message)
        {
            _logger.LogDebug("アイテム上移動要求");
            MoveUp();
        }

        public void Receive(DownMessage message)
        {
            _logger.LogDebug("アイテム下移動要求");
            MoveDown();
        }

        public void Receive(DeleteMessage message)
        {
            _logger.LogDebug("アイテム削除要求");
            DeleteSelectedCommand();
        }

        public void Receive(UndoMessage message)
        {
            _logger.LogDebug("Undo要求");
            UndoCommand();
        }

        public void Receive(RedoMessage message)
        {
            _logger.LogDebug("Redo要求");
            RedoCommand();
        }

        public void Receive(LoadMessage message)
        {
            _logger.LogDebug("ファイル読み込み要求");
            _ = LoadFileCommandAsync();
        }

        public void Receive(SaveMessage message)
        {
            _logger.LogDebug("ファイル保存要求");
            _ = SaveFileCommandAsync();
        }

        // 実行中コマンドの表示名を更新する処理
        partial void OnCurrentExecutingItemChanged(UniversalCommandItem? value)
        {
            if (value != null)
            {
                var displayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(CurrentExecutingItem.ItemType) ?? CurrentExecutingItem.ItemType;
                CurrentCommandDescription = $"実行中: {displayName} (行 {value.LineNumber})";
            }
            else
            {
                CurrentCommandDescription = string.Empty;
            }
        }

        // 選択アイテム変更時の処理を追加
        partial void OnSelectedItemChanged(UniversalCommandItem? value)
        {
            try
            {
                _logger.LogDebug("=== ListPanel選択変更: {OldItem} -> {NewItem} ===",
                    _selectedItem?.ItemType ?? "null", value?.ItemType ?? "null");

                if (value != null)
                {
                    _logger.LogInformation("📋 コマンド選択: {ItemType} (行番号: {LineNumber}, タイプ: {ActualType})",
                        value.ItemType, value.LineNumber, value.GetType().Name);

                    // EditPanelに選択変更を通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(value));

                    // 選択アイテムを強調表示するためIsSelectedを更新
                    foreach (var item in Items)
                    {
                        item.IsSelected = (item == value);
                    }

                    StatusMessage = $"選択: {DirectCommandRegistry.DisplayOrder.GetDisplayName(value.ItemType)} (行 {value.LineNumber})";
                }
                else
                {
                    _logger.LogDebug("コマンド選択解除");

                    // EditPanelに選択解除を通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));

                    // 全ての選択状態をクリア
                    foreach (var item in Items)
                    {
                        item.IsSelected = false;
                    }

                    StatusMessage = "コマンドが選択されていません";
                }

                _logger.LogDebug("=== ListPanel選択変更完了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelectedItem変更処理中にエラー");
            }
        }

        private void AddItem(string itemType)
        {
            try
            {
                _logger.LogInformation("=== コマンド追加処理開始: {ItemType} ===", itemType);

                // 1. UniversalCommandItemとして追加を試行（DirectCommandRegistry使用）
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        universalItem.LineNumber = GetNextLineNumber();
                        universalItem.Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";

                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, universalItem);

                        SelectedItem = universalItem;
                        SelectedIndex = Items.IndexOf(universalItem);

                        // EditPanelに選択変更を通知
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(universalItem));

                        _logger.LogInformation("✅ UniversalCommandItemとして追加成功: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})",
                            itemType, universalItem.LineNumber, insertIndex);
                        StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "UniversalCommandItem作成失敗、従来のFactoryを使用: {ItemType}", itemType);
                }

                // 2. 従来のCommandListItemFactoryでフォールバック
                var newItem = _commandListItemFactory.CreateItem(itemType);
                if (newItem != null)
                {
                    newItem.LineNumber = GetNextLineNumber();
                    newItem.Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";

                    var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                    Items.Insert(insertIndex, newItem);

                    SelectedItem = newItem;
                    SelectedIndex = Items.IndexOf(newItem);

                    // EditPanelに選択変更を通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));

                    _logger.LogInformation("✅ CommandListItemFactoryで追加成功: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})",
                        itemType, newItem.LineNumber, insertIndex);
                    StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました";
                }
                else
                {
                    // 3. 最終フォールバック: UniversalCommandItem
                    try
                    {
                        var fallbackItem = new UniversalCommandItem
                        {
                            ItemType = itemType,
                            LineNumber = GetNextLineNumber(),
                            IsEnable = true,
                            Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明"
                        };

                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, fallbackItem);

                        SelectedItem = fallbackItem;
                        SelectedIndex = Items.IndexOf(fallbackItem);

                        // EditPanelに選択変更を通知
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(fallbackItem));

                        _logger.LogWarning("⚠️ UniversalCommandItemとしてフォールバック追加: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})",
                            itemType, fallbackItem.LineNumber, insertIndex);
                        StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました（汎用モード）";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ UniversalCommandItem作成も失敗: {ItemType}", itemType);
                        StatusMessage = "コマンド追加に失敗しました: " + ex.Message;
                        return;
                    }
                }

                _logger.LogInformation("=== コマンド追加処理完了: {ItemType} (総アイテム数: {TotalCount}) ===",
                    itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AddItem中にエラー: {ItemType}", itemType);
                StatusMessage = $"追加エラー: {ex.Message}";
            }
        }

        private void ClearAll()
        {
            try
            {
                Items.Clear();
                SelectedItem = null;
                SelectedIndex = -1;
                StatusMessage = "すべてのアイテムをクリアしました";
                _logger.LogDebug("全アイテムクリア完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クリア中にエラー");
                StatusMessage = $"クリアエラー: {ex.Message}";
            }
        }

        private void MoveUp()
        {
            try
            {
                if (SelectedItem != null && SelectedIndex > 0)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.RemoveAt(index);
                    Items.Insert(index - 1, SelectedItem);
                    SelectedIndex = index - 1;
                    StatusMessage = "アイテムを上に移動しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上移動中にエラー");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        private void MoveDown()
        {
            try
            {
                if (SelectedItem != null && SelectedIndex < Items.Count - 1)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.RemoveAt(index);
                    Items.Insert(index + 1, SelectedItem);
                    SelectedIndex = index + 1;
                    StatusMessage = "アイテムを下に移動しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下移動中にエラー");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        private void DeleteSelectedCommand()
        {
            try
            {
                if (SelectedItem != null)
                {
                    _logger.LogInformation("選択されたアイテムを削除: {ItemType} (行番号: {LineNumber})", SelectedItem.ItemType, SelectedItem.LineNumber);

                    // 削除履歴に追加
                    UndoStack.Insert(0, SelectedItem.ItemType);
                    if (UndoStack.Count > 10) UndoStack.RemoveAt(10); // 最大10件

                    Items.Remove(SelectedItem);
                    SelectedItem = null;
                    SelectedIndex = -1;
                    StatusMessage = "選択されたアイテムを削除しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "削除中にエラー");
                StatusMessage = $"削除エラー: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoCommand()
        {
            try
            {
                if (CanUndo)
                {
                    var latestUndo = UndoStack[0];
                    UndoStack.RemoveAt(0);

                    // Redoスタックに追加
                    RedoStack.Insert(0, latestUndo);
                    if (RedoStack.Count > 10) RedoStack.RemoveAt(10); // 最大10件

                    // コマンドを復元
                    AddItem(latestUndo);

                    StatusMessage = "操作を元に戻しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Undo中にエラー");
                StatusMessage = $"Undoエラー: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void RedoCommand()
        {
            try
            {
                if (CanRedo)
                {
                    var latestRedo = RedoStack[0];
                    RedoStack.RemoveAt(0);

                    // Undoスタックに戻す
                    UndoStack.Insert(0, latestRedo);
                    if (UndoStack.Count > 10) UndoStack.RemoveAt(10); // 最大10件

                    // コマンドを再実行
                    AddItem(latestRedo);

                    StatusMessage = "操作をやり直しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redo中にエラー");
                StatusMessage = $"Redoエラー: {ex.Message}";
            }
        }

        private async Task LoadFileCommandAsync()
        {
            try
            {
                _logger.LogInformation("マクロファイル読み込み開始");
                StatusMessage = "ファイルを読み込み中...";

                var result = await _macroFileService.ShowLoadFileDialogAsync();
                
                if (!result.Success)
                {
                    StatusMessage = result.ErrorMessage ?? "ファイル読み込みに失敗しました";
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] エラー: {result.ErrorMessage}");
                    }
                    return;
                }

                if (result.Items.Any())
                {
                    // 既存のアイテムをクリア
                    Items.Clear();

                    // UIスレッドでアイテムを追加
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var item in result.Items)
                        {
                            Items.Add(item);
                        }

                        // 最初のアイテムを選択
                        if (Items.Count > 0)
                        {
                            SelectedItem = Items[0];
                            SelectedIndex = 0;
                        }
                    });

                    // ファイル名を更新
                    if (!string.IsNullOrEmpty(result.FilePath))
                    {
                        CurrentFileName = Path.GetFileNameWithoutExtension(result.FilePath);
                    }
                    else if (result.Metadata != null && !string.IsNullOrEmpty(result.Metadata.Name))
                    {
                        CurrentFileName = result.Metadata.Name;
                    }

                    HasUnsavedChanges = false;

                    StatusMessage = $"ファイル読み込み完了: {result.Items.Count}個のコマンドを読み込みました";
                    ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] ファイル読み込み完了: {result.FilePath}");
                    
                    _logger.LogInformation("マクロファイル読み込み完了: {FilePath}, Commands={Count}", 
                        result.FilePath, result.Items.Count);

                    // メタデータ情報をログに記録
                    if (result.Metadata != null)
                    {
                        _logger.LogInformation("マクロメタデータ: Name={Name}, Author={Author}, Version={Version}",
                            result.Metadata.Name, result.Metadata.Author, result.Metadata.Version);
                    }
                }
                else
                {
                    StatusMessage = "有効なコマンドが見つかりませんでした";
                    _logger.LogWarning("有効なコマンドが見つかりませんでした: {FilePath}", result.FilePath);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"ファイル読み込みエラー: {ex.Message}";
                _logger.LogError(ex, "ファイル読み込み中にエラー");
                StatusMessage = errorMsg;
                ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] エラー: {errorMsg}");
            }
        }

        private async Task SaveFileCommandAsync()
        {
            try
            {
                _logger.LogInformation("マクロファイル保存開始");
                StatusMessage = "ファイルを保存中...";

                // メタデータを作成
                var metadata = new MacroFileMetadata
                {
                    Name = CurrentFileName,
                    Description = $"AutoTool マクロファイル ({Items.Count}個のコマンド)",
                    Author = Environment.UserName,
                    CommandCount = Items.Count
                };

                var result = await _macroFileService.ShowSaveFileDialogAsync(Items, CurrentFileName);

                if (!result.Success)
                {
                    StatusMessage = result.ErrorMessage ?? "ファイル保存に失敗しました";
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] エラー: {result.ErrorMessage}");
                    }
                    return;
                }

                // ファイル名を更新
                if (!string.IsNullOrEmpty(result.FilePath))
                {
                    CurrentFileName = Path.GetFileNameWithoutExtension(result.FilePath);
                }

                HasUnsavedChanges = false;

                StatusMessage = $"ファイル保存完了: {Items.Count}個のコマンドを保存しました";
                ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] ファイル保存完了: {result.FilePath}");
                
                _logger.LogInformation("マクロファイル保存完了: {FilePath}, Commands={Count}", 
                    result.FilePath, Items.Count);
            }
            catch (Exception ex)
            {
                var errorMsg = $"ファイル保存エラー: {ex.Message}";
                _logger.LogError(ex, "ファイル保存中にエラー");
                StatusMessage = errorMsg;
                ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] エラー: {errorMsg}");
            }
        }

        private async Task ExecuteMacroAsync()
        {
            // グローバル実行チェック
            lock (_globalExecutionLock)
            {
                if (!_globalExecutionInProgress)
                {
                    _logger.LogWarning("グローバル実行フラグが設定されていないため、ExecuteMacroAsyncをスキップ");
                    return;
                }
            }

            // 排他制御: 同時実行を防止
            if (!await _executionSemaphore.WaitAsync(50))
            {
                _logger.LogWarning("マクロ実行要求: 既に実行中のため処理をスキップします");
                StatusMessage = "マクロは既に実行中です";
                return;
            }

            var executionId = Guid.NewGuid();
            _logger.LogInformation("マクロ実行開始 [ID: {ExecutionId}]: {ItemCount}個のコマンド", executionId, Items.Count);

            try
            {
                if (Items.Count == 0)
                {
                    StatusMessage = "実行するコマンドがありません";
                    _logger.LogWarning("マクロ実行要求 [ID: {ExecutionId}]: コマンドリストが空です", executionId);
                    return;
                }

                if (_macroExecutionService.IsRunning)
                {
                    _logger.LogWarning("マクロ実行要求 [ID: {ExecutionId}]: MacroExecutionServiceが既に実行中です", executionId);
                    StatusMessage = "マクロは既に実行中です";
                    return;
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // MacroExecutionServiceを使用してマクロを実行
                var result = await _macroExecutionService.StartAsync(Items);

                stopwatch.Stop();
                ElapsedTime = stopwatch.Elapsed;

                if (result)
                {
                    ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行完了 [ID: {executionId:N}] (実行時間: {ElapsedTime:mm\\:ss})");
                    _logger.LogInformation("マクロ実行成功 [ID: {ExecutionId}]: 実行時間 {ElapsedTime}", executionId, ElapsedTime);
                    Progress = 100;
                }
                else
                {
                    ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行が失敗または中断されました [ID: {executionId:N}] (実行時間: {ElapsedTime:mm\\:ss})");
                    _logger.LogWarning("マクロ実行失敗または中断 [ID: {ExecutionId}]: 実行時間 {ElapsedTime}", executionId, ElapsedTime);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"マクロ実行エラー: {ex.Message}";
                ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] エラー [ID: {executionId:N}]: {ex.Message}");
                _logger.LogError(ex, "マクロ実行中にエラーが発生 [ID: {ExecutionId}]", executionId);
            }
            finally
            {
                if (IsRunning)
                {
                    IsRunning = false;
                    CanExecute = Items.Count > 0;
                    _logger.LogDebug("マクロ実行完了 [ID: {ExecutionId}]: 実行状態をリセットしました", executionId);
                }

                _logger.LogInformation("マクロ実行処理が完全に終了しました [ID: {ExecutionId}]", executionId);
                _executionSemaphore.Release();
            }
        }


        /// <summary>
        /// リソース解放（IDisposableパターン対応）
        /// </summary>
        public void Dispose()
        {
            try
            {
                // メッセージング登録を解除
                WeakReferenceMessenger.Default.UnregisterAll(this);
                
                // MacroExecutionServiceのイベント購読解除
                _macroExecutionService.StatusChanged -= OnMacroExecutionStatusChanged;
                _macroExecutionService.RunningStateChanged -= OnMacroExecutionRunningStateChanged;
                _macroExecutionService.CommandCountChanged -= OnMacroExecutionCommandCountChanged;
                _macroExecutionService.ExecutedCountChanged -= OnMacroExecutionExecutedCountChanged;
                _macroExecutionService.ProgressChanged -= OnMacroExecutionProgressChanged;
                
                // セマフォを解放
                _executionSemaphore?.Dispose();
                
                _logger.LogInformation("ListPanelViewModel リソース解放完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanelViewModel リソース解放中にエラー");
            }
        }

        private void StopMacro()
        {
            try
            {
                _logger.LogInformation("マクロ停止要求");

                if (_macroExecutionService.IsRunning)
                {
                    _ = _macroExecutionService.StopAsync();
                    ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] マクロ停止要求");
                }
                else
                {
                    StatusMessage = "マクロは実行されていません";
                    _logger.LogWarning("マクロ停止要求: マクロは実行されていません");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ停止中にエラー");
                StatusMessage = $"停止エラー: {ex.Message}";
            }
        }

        private int GetNextLineNumber()
        {
            return Items.Count > 0 ? Items.Max(x => x.LineNumber) + 1 : 1;
        }

        /// <summary>
        /// MacroExecutionServiceのステータス変更イベントハンドラー
        /// </summary>
        private void OnMacroExecutionStatusChanged(object? sender, string status)
        {
            StatusMessage = status;
        }

        /// <summary>
        /// MacroExecutionServiceの実行状態変更イベントハンドラー
        /// </summary>
        private void OnMacroExecutionRunningStateChanged(object? sender, bool isRunning)
        {
            IsRunning = isRunning;
            CanExecute = !isRunning && Items.Count > 0;
        }

        /// <summary>
        /// MacroExecutionServiceのコマンド数変更イベントハンドラー
        /// </summary>
        private void OnMacroExecutionCommandCountChanged(object? sender, int count)
        {
            _logger.LogDebug("MacroExecutionService コマンド数更新: {Count}", count);
        }

        /// <summary>
        /// MacroExecutionServiceの実行済みコマンド数変更イベントハンドラー
        /// </summary>
        private void OnMacroExecutionExecutedCountChanged(object? sender, int count)
        {
            ExecutedItems = count;
            _logger.LogDebug("MacroExecutionService 実行済み数更新: {Count}", count);
        }

        /// <summary>
        /// MacroExecutionServiceのプログレス変更イベントハンドラー
        /// </summary>
        private void OnMacroExecutionProgressChanged(object? sender, double progress)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress = progress;
                    _logger.LogTrace("MacroExecutionService プログレス更新: {Progress:F1}%", progress);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MacroExecutionService プログレス更新中にエラー");
            }
        }

        /// <summary>
        /// コマンド開始メッセージのハンドラー
        /// </summary>
        private void OnStartCommandMessage(object recipient, StartCommandMessage message)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var targetItem = Items.FirstOrDefault(item => item.LineNumber == message.Command.LineNumber);
                    if (targetItem != null)
                    {
                        targetItem.IsRunning = true;
                        CurrentExecutingItem = targetItem;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartCommandMessage処理中にエラー");
            }
        }

        /// <summary>
        /// コマンド終了メッセージのハンドラー
        /// </summary>
        private void OnFinishCommandMessage(object recipient, FinishCommandMessage message)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var targetItem = Items.FirstOrDefault(item => item.LineNumber == message.Command.LineNumber);
                    if (targetItem != null)
                    {
                        targetItem.IsRunning = false;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FinishCommandMessage処理中にエラー");
            }
        }

        /// <summary>
        /// コマンド実行中メッセージのハンドラー
        /// </summary>
        private void OnDoingCommandMessage(object recipient, DoingCommandMessage message)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrEmpty(message.Detail))
                    {
                        ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] {message.Detail}");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoingCommandMessage処理中にエラー");
            }
        }

        /// <summary>
        /// プログレス更新メッセージのハンドラー
        /// </summary>
        private void OnUpdateProgressMessage(object recipient, UpdateProgressMessage message)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var targetItem = Items.FirstOrDefault(item => item.LineNumber == message.Command.LineNumber);
                    if (targetItem != null)
                    {
                        targetItem.Progress = message.Progress;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProgressMessage処理中にエラー");
            }
        }

        /// <summary>
        /// コマンドエラーメッセージのハンドラー
        /// </summary>
        private void OnCommandErrorMessage(object recipient, CommandErrorMessage message)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var targetItem = Items.FirstOrDefault(item => item.LineNumber == message.Command.LineNumber);
                    if (targetItem != null)
                    {
                        targetItem.IsRunning = false;
                        ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] エラー: {targetItem.ItemType} (行 {targetItem.LineNumber}) - {message.Exception.Message}");
                    }
                    StatusMessage = $"コマンドエラー: {message.Exception.Message}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandErrorMessage処理中にエラー");
            }
        }

        /// <summary>
        /// マクロ実行コマンド
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecute))]
        private async Task RunMacroAsync()
        {
            if (!CanExecute) return;

            try
            {
                _logger.LogInformation("RunMacroAsyncコマンド実行開始");
                WeakReferenceMessenger.Default.Send(new RunMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacroAsyncコマンド実行中にエラー");
            }
        }

        /// <summary>
        /// マクロ停止コマンド
        /// </summary>
        [RelayCommand]
        private void StopMacroCommand()
        {
            StopMacro();
        }
    }
}