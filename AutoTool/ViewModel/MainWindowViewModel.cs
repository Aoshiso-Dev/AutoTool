using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.Services;
using AutoTool.Services.Plugin;
using AutoTool.Services.UI;
using AutoTool.Services.MacroFile;
using AutoTool.Services.Execution;
using AutoTool.ViewModel.Panels;
using AutoTool.ViewModel.Shared;
using AutoTool.Message;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Command.Definition;
using System.Threading;
using System.Collections.Specialized;
using System.Windows;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// MainWindowViewModel (DirectCommandRegistry統合版) - 真の一元化（マクロ実行処理統合）
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject, 
        IRecipient<LoadMessage>, 
        IRecipient<SaveMessage>, 
        IRecipient<RunMessage>,
        IRecipient<StopMessage>
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly RecentFileService _recentFileService;
        private readonly IPluginService _pluginService;
        private readonly IMainWindowMenuService _menuService;
        private readonly IMacroFileService _macroFileService;
        private readonly IMacroExecutionService _macroExecutionService;

        // 重複処理防止フラグ
        private bool _isProcessingLoad = false;
        private bool _isProcessingSave = false;
        private bool _isProcessingRun = false;

        // マクロ実行制御用
        private static volatile bool _globalExecutionInProgress = false;
        private static readonly object _globalExecutionLock = new object();
        private readonly SemaphoreSlim _executionSemaphore = new SemaphoreSlim(1, 1);

        private readonly Guid _instanceId = Guid.NewGuid();

        [ObservableProperty]
        private string _title = "AutoTool - 自動化ツール";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _availableCommands = new();

        [ObservableProperty]
        private double _windowWidth = 1200;

        [ObservableProperty]
        private double _windowHeight = 800;

        [ObservableProperty]
        private bool _isMaximized = false;

        // ViewModelプロパティ（直接参照回避のためメッセージング経由で制御）
        public ListPanelViewModel ListPanelViewModel { get; }
        public EditPanelViewModel EditPanelViewModel { get; }
        public ButtonPanelViewModel ButtonPanelViewModel { get; }

        // 統計プロパティ（メッセージング経由で取得）
        [ObservableProperty]
        private int _commandCount = 0;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private double _progress = 0.0;

        [ObservableProperty]
        private int _executedItems = 0;

        [ObservableProperty]
        private TimeSpan _elapsedTime;

        public bool HasCommands => CommandCount > 0;
        public bool CanExecute => !IsRunning && HasCommands;

        [RelayCommand]
        private void RefreshPerformance()
        {
            StatusMessage = "パフォーマンス情報を更新しました";
        }

        [RelayCommand]
        private void LoadPluginFile()
        {
            StatusMessage = "プラグイン読み込み機能は未実装です";
        }

        [RelayCommand]
        private void RefreshPlugins()
        {
            StatusMessage = "プラグイン再読み込み機能は未実装です";
        }

        [RelayCommand]
        private void ShowPluginInfo()
        {
            StatusMessage = "プラグイン情報表示機能は未実装です";
        }

        [RelayCommand]
        private void OpenAppDir()
        {
            StatusMessage = "アプリケーションフォルダを開く機能は未実装です";
        }

        [RelayCommand]
        private void ClearLog()
        {
            try
            {
                // Clear MainWindow entries on UI thread
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    LogEntries.Clear();
                });

                // Also clear ListPanelViewModel execution log if available
                if (ListPanelViewModel?.ExecutionLog != null)
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ListPanelViewModel.ExecutionLog.Clear();
                    });
                }

                StatusMessage = "ログをクリアしました";
                _logger.LogInformation("ユーザーによってログがクリアされました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログクリア中にエラー");
                StatusMessage = $"ログクリアエラー: {ex.Message}";
            }
        }

        // プロパティ（バインディングエラー回避用）
        public string MenuItemHeader_SaveFile => "保存(_S)";
        public string MenuItemHeader_SaveFileAs => "名前を付けて保存(_A)";
        public ObservableCollection<object> RecentFiles { get; } = new();
        public ObservableCollection<string> LogEntries { get; } = new();
        // Detailed log (ILogger output) from in-memory logger
        public ObservableCollection<string> DetailedLogEntries { get; }
        private readonly Services.Logging.LogMessageService? _logMessageService;
        public string MemoryUsage => "0 MB";
        public string CpuUsage => "0%";
        public int PluginCount => 0;

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            RecentFileService recentFileService,
            IPluginService pluginService,
            IMainWindowMenuService menuService,
            IMacroFileService macroFileService,
            IMacroExecutionService macroExecutionService,
            ListPanelViewModel listPanelViewModel,
            EditPanelViewModel editPanelViewModel,
            ButtonPanelViewModel buttonPanelViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _macroFileService = macroFileService ?? throw new ArgumentNullException(nameof(macroFileService));
            _macroExecutionService = macroExecutionService ?? throw new ArgumentNullException(nameof(macroExecutionService));
            
            ListPanelViewModel = listPanelViewModel ?? throw new ArgumentNullException(nameof(listPanelViewModel));
            EditPanelViewModel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
            ButtonPanelViewModel = buttonPanelViewModel ?? throw new ArgumentNullException(nameof(buttonPanelViewModel));

            _logger.LogInformation("MainWindowViewModel (DirectCommandRegistry版) 初期化開始 - 真の一元化（マクロ実行処理統合） (Instance={InstanceId})", _instanceId);

            // メッセージ処理の一元化: MainWindowViewModelでLoad/Save/Runを処理
            WeakReferenceMessenger.Default.Register<LoadMessage>(this);
            WeakReferenceMessenger.Default.Register<SaveMessage>(this);
            WeakReferenceMessenger.Default.Register<RunMessage>(this);
            WeakReferenceMessenger.Default.Register<StopMessage>(this);

            // CommandCountの更新を監視（メッセージング経由）
            WeakReferenceMessenger.Default.Register<CommandCountChangedMessage>(this, (r, m) =>
            {
                CommandCount = m.Count;
                OnPropertyChanged(nameof(HasCommands));
                OnPropertyChanged(nameof(CanExecute));
            });

            // MacroExecutionServiceのイベント購読（実行状態の同期）
            _macroExecutionService.StatusChanged += OnMacroExecutionStatusChanged;
            _macroExecutionService.RunningStateChanged += OnMacroExecutionRunningStateChanged;
            _macroExecutionService.CommandCountChanged += OnMacroExecutionCommandCountChanged;
            _macroExecutionService.ExecutedCountChanged += OnMacroExecutionExecutedCountChanged;
            _macroExecutionService.ProgressChanged += OnMacroExecutionProgressChanged;

            // Register execution log binding so ListPanel's ExecutionLog shows up in MainWindow's LogEntries
            RegisterExecutionLogBinding();

            // Try to obtain LogMessageService for detailed logger view
            _logMessageService = serviceProvider.GetService<Services.Logging.LogMessageService>();
            if (_logMessageService != null)
            {
                DetailedLogEntries = _logMessageService.Messages;
            }
            else
            {
                DetailedLogEntries = new ObservableCollection<string>();
            }

            Initialize();
        }

        private void RegisterExecutionLogBinding()
        {
            try
            {
                if (ListPanelViewModel?.ExecutionLog == null) return;

                // Copy existing entries
                //System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                //{
                //    foreach (var entry in ListPanelViewModel.ExecutionLog)
                //    {
                //        LogEntries.Add(entry);
                //    }
                //});

                // Subscribe to changes
                ListPanelViewModel.ExecutionLog.CollectionChanged += ExecutionLog_CollectionChanged;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ExecutionLog バインド登録中に警告が発生しました");
            }
        }

        private void ExecutionLog_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            if (e.NewItems != null)
                            {
                                foreach (var item in e.NewItems.OfType<string>())
                                    LogEntries.Add(item);
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            if (e.OldItems != null)
                            {
                                foreach (var item in e.OldItems.OfType<string>())
                                    LogEntries.Remove(item);
                            }
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            LogEntries.Clear();
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            if (e.NewItems != null && e.OldItems != null && e.OldItems.Count == e.NewItems.Count)
                            {
                                int start = e.OldStartingIndex >= 0 ? e.OldStartingIndex : 0;
                                for (int i = 0; i < e.NewItems.Count; i++)
                                {
                                    var newItem = e.NewItems[i] as string;
                                    var idx = start + i;
                                    if (idx >= 0 && idx < LogEntries.Count && newItem != null)
                                        LogEntries[idx] = newItem;
                                }
                            }
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecutionLog 変更処理中にエラーが発生しました");
            }
        }

        #region メッセージ処理 (一元化・VM直接参照回避)

        /// <summary>
        /// ファイル読み込みメッセージ処理
        /// </summary>
        public void Receive(LoadMessage message)
        {
            if (_isProcessingLoad)
            {
                _logger.LogWarning("Load処理が既に実行中です - MainWindowViewModel");
                StatusMessage = "読み込み処理は既に実行中です";
                return;
            }
            _isProcessingLoad = true;

            try
            {
                _logger.LogInformation("MainWindowViewModel でLoad処理開始: FilePath={FilePath}", message.FilePath ?? "null");
                StatusMessage = "ファイルを読み込み中...";

                // ListPanelViewModelに直接アクセスする代わりに、専用メッセージを送信
                WeakReferenceMessenger.Default.Send(new InternalLoadMessage(message.FilePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load処理中にエラー - MainWindowViewModel");
                StatusMessage = $"読み込みエラー: {ex.Message}";
            }
            finally
            {
                Task.Delay(500).ContinueWith(_ => _isProcessingLoad = false);
            }
        }

        /// <summary>
        /// ファイル保存メッセージ処理
        /// </summary>
        public void Receive(SaveMessage message)
        {
            if (_isProcessingSave)
            {
                _logger.LogWarning("Save処理が既に実行中です - MainWindowViewModel");
                StatusMessage = "保存処理は既に実行中です";
                return;
            }
            _isProcessingSave = true;

            try
            {
                _logger.LogInformation("MainWindowViewModel でSave処理開始: FilePath={FilePath}", message.FilePath ?? "null");
                StatusMessage = "ファイルを保存中...";

                // ListPanelViewModelに直接アクセスする代わりに、専用メッセージを送信
                WeakReferenceMessenger.Default.Send(new InternalSaveMessage(message.FilePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save処理中にエラー - MainWindowViewModel");
                StatusMessage = $"保存エラー: {ex.Message}";
            }
            finally
            {
                Task.Delay(500).ContinueWith(_ => _isProcessingSave = false);
            }
        }

        /// <summary>
        /// マクロ実行メッセージ処理（真の一元化）
        /// </summary>
        public void Receive(RunMessage message)
        {
            // グローバル実行チェック
            lock (_globalExecutionLock)
            {
                if (_globalExecutionInProgress)
                {
                    _logger.LogWarning("グローバル実行中のためRunMessage受信をブロック - MainWindowViewModel");
                    StatusMessage = "マクロは既に実行中です";
                    return;
                }
            }

            if (_isProcessingRun)
            {
                _logger.LogWarning("Run処理が既に実行中です - MainWindowViewModel");
                StatusMessage = "実行処理は既に実行中です";
                return;
            }
            _isProcessingRun = true;

            try
            {
                _logger.LogInformation("MainWindowViewModel でRun処理開始（真の一元化）");
                StatusMessage = "マクロを実行中...";

                if (ListPanelViewModel.Items.Count == 0)
                {
                    StatusMessage = "実行するコマンドがありません";
                    _logger.LogWarning("RunMessage処理スキップ: コマンドリストが空です");
                    return;
                }

                if (IsRunning || _macroExecutionService.IsRunning)
                {
                    StatusMessage = "マクロは既に実行中です";
                    _logger.LogWarning("RunMessage処理スキップ: マクロは既に実行中です");
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

                _logger.LogInformation("マクロ実行開始（MainWindowViewModel一元制御）");
                
                // ButtonPanelViewModelの実行状態を同期
                ButtonPanelViewModel.SetRunningState(true);

                _ = Task.Run(async () => 
                {
                    try
                    {
                        await ExecuteMacroAsync();
                    }
                    finally
                    {
                        _isProcessingRun = false;
                        
                        // グローバル実行フラグをリセット
                        lock (_globalExecutionLock)
                        {
                            _globalExecutionInProgress = false;
                            _logger.LogDebug("グローバル実行フラグをリセットしました - MainWindowViewModel");
                        }

                        // ButtonPanelViewModelの実行状態を同期
                        ButtonPanelViewModel.SetRunningState(false);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Run処理中にエラー - MainWindowViewModel");
                StatusMessage = $"実行エラー: {ex.Message}";
                _isProcessingRun = false;
                lock (_globalExecutionLock)
                {
                    _globalExecutionInProgress = false;
                }
                ButtonPanelViewModel.SetRunningState(false);
            }
        }

        /// <summary>
        /// マクロ停止メッセージ処理（真の一元化）
        /// </summary>
        public void Receive(StopMessage message)
        {
            try
            {
                _logger.LogInformation("MainWindowViewModel でStop処理開始（真の一元化）");
                StatusMessage = "マクロを停止中...";

                // 実際の停止処理
                StopMacro();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stop処理中にエラー - MainWindowViewModel");
                StatusMessage = $"停止エラー: {ex.Message}";
            }
        }

        #endregion

        #region マクロ実行処理（MainWindowViewModelに統合）

        /// <summary>
        /// マクロ実行処理（MainWindowViewModelでの一元化）
        /// </summary>
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
            _logger.LogInformation("マクロ実行開始（MainWindowViewModel一元制御） [ID: {ExecutionId}]: {ItemCount}個のコマンド", 
                executionId, ListPanelViewModel.Items.Count);

            try
            {
                if (ListPanelViewModel.Items.Count == 0)
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
                var result = await _macroExecutionService.StartAsync(ListPanelViewModel.Items);

                stopwatch.Stop();
                ElapsedTime = stopwatch.Elapsed;

                if (result)
                {
                    _logger.LogInformation("マクロ実行成功 [ID: {ExecutionId}]: 実行時間 {ElapsedTime}", executionId, ElapsedTime);
                    Progress = 100;
                    StatusMessage = $"マクロ実行完了 (実行時間: {ElapsedTime:mm\\:ss})";
                }
                else
                {
                    _logger.LogWarning("マクロ実行失敗または中断 [ID: {ExecutionId}]: 実行時間 {ElapsedTime}", executionId, ElapsedTime);
                    StatusMessage = $"マクロ実行が失敗または中断されました (実行時間: {ElapsedTime:mm\\:ss})";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"マクロ実行エラー: {ex.Message}";
                _logger.LogError(ex, "マクロ実行中にエラーが発生 [ID: {ExecutionId}]", executionId);
            }
            finally
            {
                if (IsRunning)
                {
                    IsRunning = false;
                    OnPropertyChanged(nameof(CanExecute));
                    _logger.LogDebug("マクロ実行完了 [ID: {ExecutionId}]: 実行状態をリセットしました", executionId);
                }

                _logger.LogInformation("マクロ実行処理が完全に終了しました [ID: {ExecutionId}]", executionId);
                _executionSemaphore.Release();
            }
        }

        /// <summary>
        /// マクロ停止処理（MainWindowViewModelでの一元化）
        /// </summary>
        private void StopMacro()
        {
            try
            {
                _logger.LogInformation("マクロ停止要求（MainWindowViewModel一元制御）");

                if (_macroExecutionService.IsRunning)
                {
                    _ = _macroExecutionService.StopAsync();
                    StatusMessage = "マクロ停止要求を送信しました";
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

        #endregion

        #region MacroExecutionServiceイベントハンドラー

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
            OnPropertyChanged(nameof(CanExecute));
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

        #endregion

        private void Initialize()
        {
            try
            {
                LoadAvailableCommands();
                StatusMessage = "初期化完了 - 真の一元化（マクロ実行処理統合）";
                _logger.LogInformation("MainWindowViewModel 初期化完了 - 真の一元化（マクロ実行処理統合）");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainWindowViewModel 初期化中にエラー");
                StatusMessage = $"初期化エラー: {ex.Message}";
            }
        }

        private void LoadAvailableCommands()
        {
            try
            {
                _logger.LogDebug("利用可能なコマンドの読み込み開始");
                
                var displayItems = AutoToolCommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = AutoToolCommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = AutoToolCommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                AvailableCommands = new ObservableCollection<CommandDisplayItem>(displayItems);
                
                _logger.LogDebug("利用可能なコマンド読み込み完了: {Count}個", AvailableCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "利用可能なコマンド読み込み中にエラー");
                AvailableCommands = new ObservableCollection<CommandDisplayItem>();
            }
        }

        [RelayCommand]
        private async Task LoadFileAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "ファイルを読み込み中...";
                
                // MainWindowVMでの一元化処理
                WeakReferenceMessenger.Default.Send(new LoadMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラー");
                StatusMessage = $"読み込みエラー: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveFileAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "ファイルを保存中...";
                
                // MainWindowVMでの一元化処理
                WeakReferenceMessenger.Default.Send(new SaveMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラー");
                StatusMessage = $"保存エラー: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Exit()
        {
            try
            {
                _logger.LogInformation("アプリケーション終了要求");
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アプリケーション終了中にエラー");
            }
        }

        [RelayCommand]
        private void ShowAbout()
        {
            try
            {
                _logger.LogDebug("About ダイアログ表示");
                StatusMessage = "AutoTool v1.0 - 自動化ツール";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "About ダイアログ表示中にエラー");
            }
        }

        public void SetStatus(string message)
        {
            StatusMessage = message;
            _logger.LogDebug("ステータス更新: {Message}", message);
        }

        public void SetLoading(bool isLoading)
        {
            IsLoading = isLoading;
            if (isLoading)
            {
                StatusMessage = "処理中...";
            }
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            _logger.LogTrace("プロパティ変更: {PropertyName}", e.PropertyName);
        }

        // ClearLog now also clears detailed logger contents
        [RelayCommand]
        private void ClearDetailedLog()
        {
            try
            {
                _logMessageService?.Clear();
                DetailedLogEntries.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "詳細ログクリア中にエラー");
            }
        }

        [RelayCommand]
        private void CopyLog()
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (LogEntries == null || LogEntries.Count == 0)
                    {
                        StatusMessage = "コピーする実行ログがありません";
                        return;
                    }

                    var text = string.Join(Environment.NewLine, LogEntries);
                    System.Windows.Clipboard.SetText(text);
                    StatusMessage = "実行ログをクリップボードにコピーしました";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行ログのコピー中にエラー");
                StatusMessage = $"コピーエラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CopyDetailedLog()
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (DetailedLogEntries == null || DetailedLogEntries.Count == 0)
                    {
                        StatusMessage = "コピーする詳細ログがありません";
                        return;
                    }

                    var text = string.Join(Environment.NewLine, DetailedLogEntries);
                    System.Windows.Clipboard.SetText(text);
                    StatusMessage = "詳細ログをクリップボードにコピーしました";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "詳細ログのコピー中にエラー");
                StatusMessage = $"コピーエラー: {ex.Message}";
            }
        }
    }
}