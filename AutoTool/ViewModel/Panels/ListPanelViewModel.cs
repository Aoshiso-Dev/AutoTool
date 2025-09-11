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
using AutoTool.Command.Base;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// ListPanelViewModel (Phase 5統合版) - マクロ実行をMainWindowViewModelに移譲
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject, 
        IRecipient<AddMessage>, IRecipient<ClearMessage>, 
        IRecipient<UpMessage>, IRecipient<DownMessage>, IRecipient<DeleteMessage>, IRecipient<UndoMessage>, IRecipient<RedoMessage>,
        IRecipient<InternalLoadMessage>, IRecipient<InternalSaveMessage>,
        IDisposable
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

        private IAutoToolCommand? _currentExecutingCommand;
        private CancellationTokenSource? _cancellationTokenSource;
        
        // ファイル操作用セマフォ
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);

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
            
            WeakReferenceMessenger.Default.Register<AddMessage>(this);
            WeakReferenceMessenger.Default.Register<ClearMessage>(this);
            WeakReferenceMessenger.Default.Register<UpMessage>(this);
            WeakReferenceMessenger.Default.Register<DownMessage>(this);
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this);
            WeakReferenceMessenger.Default.Register<UndoMessage>(this);
            WeakReferenceMessenger.Default.Register<RedoMessage>(this);
            // MainWindowViewModelからの内部メッセージを受信
            WeakReferenceMessenger.Default.Register<InternalLoadMessage>(this);
            WeakReferenceMessenger.Default.Register<InternalSaveMessage>(this);
            // Run/StopメッセージはMainWindowViewModelで処理

            // プログレス表示とハイライト用のメッセージ登録
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, OnStartCommandMessage);
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, OnFinishCommandMessage);
            WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, OnDoingCommandMessage);
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, OnUpdateProgressMessage);

            // CommandErrorMessage は受信されているかをトレースするため、ラップして登録
            // 受信登録時点での受信者数をログ出力（診断用）
            try
            {
                try
                {
                    var messengerType = WeakReferenceMessenger.Default.GetType();
                    var recipientsField = messengerType.GetField("recipients", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (recipientsField?.GetValue(WeakReferenceMessenger.Default) is System.Collections.IDictionary recipients)
                    {
                        _logger.LogDebug("Messenger 登録時の recipients カウント: {Count}", recipients.Count);
                    }
                }
                catch { /* ignore */ }

                WeakReferenceMessenger.Default.Register<CommandErrorMessage>(this, (recipient, message) =>
                {
                    try
                    {
                        _logger.LogDebug("CommandErrorMessage 受信トレース: Line={LineNumber}, CommandType={CommandType}, Exception={ExceptionType}",
                            message.Command?.LineNumber, message.Command?.GetType().Name, message.Exception?.GetType().Name);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "CommandErrorMessage 受信トレースログで例外発生");
                    }

                    // 既存のハンドラを呼ぶ
                    OnCommandErrorMessage(recipient, message);
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CommandErrorMessage の登録トレース処理中に例外が発生しました");
                // 通常登録にフォールバック
                WeakReferenceMessenger.Default.Register<CommandErrorMessage>(this, OnCommandErrorMessage);
            }

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

                // MainWindowViewModelにコマンド数変更を通知
                WeakReferenceMessenger.Default.Send(new CommandCountChangedMessage(Items.Count));
            };

            // 初期サンプルコマンドを追加
            TryAddSampleCommand();
        }

        /// <summary>
        /// サンプルWaitコマンドを追加（デモ用）
        /// </summary>
        private void TryAddSampleCommand()
        {
            try
            {
                _logger.LogDebug("サンプルコマンドの追加を試行します（ListPanelViewModel）");
                
                // Waitコマンドを追加
                AddItem("wait");
                
                _logger.LogInformation("サンプルWaitコマンドをListPanelに追加しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "サンプルコマンド追加中にエラーが発生しました（ListPanelViewModel）");
            }
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

        // 内部メッセージ受信処理（MainWindowViewModelからの一元化メッセージ）

        public void Receive(InternalLoadMessage message)
        {
            _logger.LogDebug("InternalLoadMessage受信: FilePath={FilePath}", message.FilePath ?? "null");
            _ = LoadFileCommandAsync();
        }

        public void Receive(InternalSaveMessage message)
        {
            _logger.LogDebug("InternalSaveMessage受信: FilePath={FilePath}", message.FilePath ?? "null");
            _ = SaveFileCommandAsync();
        }

        // 実行中コマンドの表示名を更新する処理
        partial void OnCurrentExecutingItemChanged(UniversalCommandItem? value)
        {
            if (value != null)
            {
                var displayName = AutoToolCommandRegistry.DisplayOrder.GetDisplayName(CurrentExecutingItem.ItemType) ?? CurrentExecutingItem.ItemType;
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

                    StatusMessage = $"選択: {AutoToolCommandRegistry.DisplayOrder.GetDisplayName(value.ItemType)} (行 {value.LineNumber})";
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
                    var universalItem = AutoToolCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        // Sanitize window-related settings to avoid boolean false being shown
                        SanitizeWindowSettings(universalItem);

                        universalItem.LineNumber = GetNextLineNumber();
                        universalItem.Comment = $"{AutoToolCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";

                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, universalItem);

                        SelectedItem = universalItem;
                        SelectedIndex = Items.IndexOf(universalItem);

                        // EditPanelに選択変更を通知
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(universalItem));

                        _logger.LogInformation("✅ UniversalCommandItemとして追加成功: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})",
                            itemType, universalItem.LineNumber, insertIndex);
                        StatusMessage = $"{AutoToolCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました";
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
                    // Sanitize if UniversalCommandItem-like
                    if (newItem is UniversalCommandItem newUniversal)
                    {
                        SanitizeWindowSettings(newUniversal);
                    }

                    newItem.LineNumber = GetNextLineNumber();
                    newItem.Comment = $"{AutoToolCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";

                    var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                    Items.Insert(insertIndex, newItem);

                    SelectedItem = newItem;
                    SelectedIndex = Items.IndexOf(newItem);

                    // EditPanelに選択変更を通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));

                    _logger.LogInformation("✅ CommandListItemFactoryで追加成功: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})",
                        itemType, newItem.LineNumber, insertIndex);
                    StatusMessage = $"{AutoToolCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました";
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
                            Comment = $"{AutoToolCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明"
                        };

                        // Ensure window settings are empty strings instead of boolean false
                        SanitizeWindowSettings(fallbackItem);

                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, fallbackItem);

                        SelectedItem = fallbackItem;
                        SelectedIndex = Items.IndexOf(fallbackItem);

                        // EditPanelに選択変更を通知
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(fallbackItem));

                        _logger.LogWarning("⚠️ UniversalCommandItemとしてフォールバック追加: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})",
                            itemType, fallbackItem.LineNumber, insertIndex);
                        StatusMessage = $"{AutoToolCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました（汎用モード）";
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

        // Ensure WindowTitle and WindowClassName are strings (empty when not set), avoid boolean false or "False" values
        private void SanitizeWindowSettings(UniversalCommandItem? item)
        {
            if (item == null) return;
            try
            {
                if (item.Settings == null) return;

                void Normalize(string key)
                {
                    if (item.Settings.TryGetValue(key, out var raw))
                    {
                        if (raw is bool b && b == false)
                        {
                            item.SetSetting(key, string.Empty);
                        }
                        else if (raw is string s && string.Equals(s, "False", StringComparison.OrdinalIgnoreCase))
                        {
                            item.SetSetting(key, string.Empty);
                        }
                        else if (raw == null)
                        {
                            item.SetSetting(key, string.Empty);
                        }
                    }
                    else
                    {
                        // If key missing, ensure it's present as empty string to maintain consistency
                        item.SetSetting(key, string.Empty);
                    }
                }

                Normalize("WindowTitle");
                Normalize("WindowClassName");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ウィンドウ設定の正規化中にエラーが発生しました");
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
                // Prefer SelectedItem reference to determine current index
                var item = SelectedItem;
                if (item == null)
                {
                    StatusMessage = "選択項目がありません";
                    return;
                }

                var index = Items.IndexOf(item);
                if (index <= 0)
                {
                    StatusMessage = "上に移動できません";
                    return;
                }

                // Use ObservableCollection.Move to preserve references and events
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Move(index, index - 1);

                    // Maintain selection after move
                    SelectedIndex = index - 1;
                    SelectedItem = Items[SelectedIndex];

                    // Sync selection flags
                    foreach (var it in Items)
                        it.IsSelected = (it == SelectedItem);
                });

                StatusMessage = "アイテムを上に移動しました";
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
                var item = SelectedItem;
                if (item == null)
                {
                    StatusMessage = "選択項目がありません";
                    return;
                }

                var index = Items.IndexOf(item);
                if (index < 0 || index >= Items.Count - 1)
                {
                    StatusMessage = "下に移動できません";
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Move(index, index + 1);

                    SelectedIndex = index + 1;
                    SelectedItem = Items[SelectedIndex];

                    foreach (var it in Items)
                        it.IsSelected = (it == SelectedItem);
                });

                StatusMessage = "アイテムを下に移動しました";
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
                _logger.LogError(ex, "アイテム削除中にエラー");
                StatusMessage = $"削除エラー: {ex.Message}";
            }
        }

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
                _fileSemaphore?.Dispose();
                
                _logger.LogInformation("ListPanelViewModel リソース解放完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanelViewModel リソース解放中にエラー");
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
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsRunning = isRunning;
                    CanExecute = !isRunning && Items.Count > 0;

                    if (!isRunning)
                    {
                        // マクロが停止/完了したタイミングで、すべてのアイテムの実行フラグとカレント実行アイテム、進捗を確実にクリア
                        try
                        {
                            foreach (var it in Items)
                            {
                                it.IsRunning = false;
                                it.Progress = 0; // 進捗をクリア
                            }

                            CurrentExecutingItem = null;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "マクロ停止時の実行フラグ/進捗クリア中に警告");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MacroExecutionService 実行状態変更処理中にエラー");
            }
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
                        targetItem.Progress = 0; // 完了時に個別進捗をクリアしてUIのバーを消す

                        // 現在実行中として記録されているアイテムが完了した場合はカレントをクリア
                        if (CurrentExecutingItem == targetItem)
                        {
                            CurrentExecutingItem = null;
                        }
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
                    var now = DateTime.Now;
                    var ex = message.Exception;
                    var shortReason = GetShortErrorReason(ex, message.Command);

                    // 実行ログに中断メッセージ（例: 中断: ClickImage (行 3) - FileNotFoundException: ～）
                    if (targetItem != null)
                    {
                        targetItem.IsRunning = false;
                        targetItem.Progress = 0; // エラーでも進捗はクリアしておく

                        ExecutionLog.Add($"[{now:HH:mm:ss}] 中断: {targetItem.ItemType} (行 {targetItem.LineNumber}) - {ex.GetType().Name}: {ex.Message}");
                        ExecutionLog.Add($"[{now:HH:mm:ss}] 原因: {shortReason}");

                        // エラーで終了したアイテムがカレントの場合はクリア
                        if (CurrentExecutingItem == targetItem)
                        {
                            CurrentExecutingItem = null;
                        }
                    }
                    else
                    {
                        // 対象アイテムが見つからない場合もログは残す
                        //ExecutionLog.Add($"[{now:HH:mm:ss}] コマンドエラー: {ex.GetType().Name}: {ex.Message}");
                        //ExecutionLog.Add($"[{now:HH:mm:ss}] 原因: {shortReason}");
                    }

                    // 詳細はILoggerへ（スタックトレース等）
                    _logger.LogError(ex, "コマンドがエラーで中断しました: Line {Line}", message.Command.LineNumber);

                    // UI向けステータスメッセージ
                    StatusMessage = $"コマンドがエラーで中断しました: {ex.Message}";
                });
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "CommandErrorMessage処理中にエラー");
            }
        }

        // 短い原因説明を返す補助メソッド（コマンド種別や例外内容に基づく）
        private string GetShortErrorReason(Exception ex, IAutoToolCommand? command)
        {
            if (ex is System.IO.FileNotFoundException)
            {
                return "指定されたファイルが見つかりません。ファイルパスを確認してください。";
            }
            if (ex is System.IO.DirectoryNotFoundException)
            {
                return "指定されたフォルダが見つかりません。パスを確認してください。";
            }
            if (ex is UnauthorizedAccessException)
            {
                return "アクセス権が不足しています。ファイル/フォルダの権限を確認してください。";
            }
            if (ex is TimeoutException)
            {
                return "処理がタイムアウトしました。条件やタイムアウト設定を確認してください。";
            }
            if (ex is NullReferenceException)
            {
                return "内部参照（null）が原因で失敗しました。設定や対象オブジェクトを確認してください。";
            }
            if (ex is System.IO.IOException)
            {
                return "入出力エラーが発生しました。デバイスやファイルの状態を確認してください。";
            }

            // デフォルトは例外メッセージを短くして返す
            var msg = ex.Message ?? "不明なエラーです";
            if (msg.Length > 200) msg = msg.Substring(0, 200) + "…";
            return msg;
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

        /// <summary>
        /// ファイル読み込み処理 (MainWindowViewModel用にpublic化)
        /// </summary>
        public async Task LoadFileCommandAsync()
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

        /// <summary>
        /// ファイル保存処理 (MainWindowViewModel用にpublic化)
        /// </summary>
        public async Task SaveFileCommandAsync()
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

        [RelayCommand]
        private void SelectNode(object? node)
        {
            try 
            {
                if (node is UniversalCommandItem item)
                {
                    SelectedItem = item;
                    SelectedIndex = Items.IndexOf(item);
                    _logger.LogDebug("ノード選択: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);
                }
                else
                {
                    SelectedItem = null;
                    SelectedIndex = -1;
                    _logger.LogDebug("ノード選択解除");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ノード選択中にエラー");
            }
        }
    }
}