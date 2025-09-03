using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.ViewModel.Panels; // 追加: PanelViewModels用
using AutoTool.Message; // Message
using AutoTool.Services.Plugin; // PluginService
using AutoTool.ViewModel.Shared; // CommandHistoryManager
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.Theme;
using AutoTool.Services.Performance;
using Microsoft.Win32;
using AutoTool.Command.Interface;
using AutoTool.Model.MacroFactory;
using AutoTool.Command.Class;
using AutoTool.Model.List.Interface;
using System.Collections.Generic;

namespace AutoTool
{
    /// <summary>
    /// MainWindowViewModel
    /// マクロ実行ロジックを統合
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        // プライベートフィールド
        private readonly ILogger<MainWindowViewModel>? _logger;
        private readonly IConfigurationService? _configurationService;
        private readonly IThemeService? _themeService;
        private readonly IPerformanceService? _performanceService;
        private readonly AutoTool.Services.Plugin.IPluginService? _pluginService;
        private readonly System.IServiceProvider? _serviceProvider;
        private CancellationTokenSource? _cancellationTokenSource;
        private ICommand? _currentMacroCommand;

        // 各パネルのViewModel（DIで直接受け取る）
        [ObservableProperty]
        private ButtonPanelViewModel? _buttonPanelViewModel;
        
        [ObservableProperty]
        private ListPanelViewModel? _listPanelViewModel;
        
        [ObservableProperty]
        private EditPanelViewModel? _editPanelViewModel;
        
        [ObservableProperty]
        private LogPanelViewModel? _logPanelViewModel;
        
        [ObservableProperty]
        private FavoritePanelViewModel? _favoritePanelViewModel;

        // 実行ステータス関連
        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private int _currentCommandIndex = 0;

        [ObservableProperty]
        private int _totalCommands = 0;

        [ObservableProperty]
        private CommandExecutionStats _executionStats = new();

        [ObservableProperty]
        private double _overallProgress = 0.0;

        [ObservableProperty]
        private string _currentCommandDescription = string.Empty;

        // テーマ
        [ObservableProperty]
        private AppTheme _currentTheme = AppTheme.Light;

        // ウィンドウの状態
        [ObservableProperty]
        private double _windowWidth = 1200;

        [ObservableProperty]
        private double _windowHeight = 800;

        [ObservableProperty]
        private WindowState _windowState = WindowState.Normal;

        [ObservableProperty]
        private string _title = "AutoTool - マクロ自動化ツール";

        // パフォーマンス情報
        [ObservableProperty]
        private string _memoryUsage = "0 MB";

        [ObservableProperty]
        private string _cpuUsage = "0%";

        // プラグイン情報
        [ObservableProperty]
        private ObservableCollection<AutoTool.Services.Plugin.IPluginInfo> _loadedPlugins = new();

        [ObservableProperty]
        private ObservableCollection<AutoTool.Services.Plugin.IPluginCommandInfo> _availableCommands = new();

        [ObservableProperty]
        private int _pluginCount;

        [ObservableProperty]
        private int _commandCount;

        // タブ選択インデックス
        [ObservableProperty]
        private int _selectedTabIndex = 0;

        // ファイル関連
        private string? _currentMacroFilePath;

        // 最近使ったファイル
        private ObservableCollection<RecentFileEntry> _recentFiles = new();
        public ObservableCollection<RecentFileEntry> RecentFiles
        {
            get => _recentFiles;
            set { if (_recentFiles != value) { _recentFiles = value; OnPropertyChanged(nameof(RecentFiles)); } }
        }

        private bool _isFileOpened;
        public bool IsFileOpened
        {
            get => _isFileOpened;
            set { if (_isFileOpened != value) { _isFileOpened = value; OnPropertyChanged(nameof(IsFileOpened)); OnPropertyChanged(nameof(MenuItemHeader_SaveFile)); } }
        }

        // ファイル操作が可能か（実行中でない）
        public bool IsFileOperationEnable => !IsRunning;

        // メニュー表示用ヘッダ
        public string MenuItemHeader_SaveFile => IsFileOpened ? "上書き保存 (_S)" : "保存 (_S)";
        public string MenuItemHeader_SaveFileAs => "名前を付けて保存 (_A)";

        // 最近ファイル用エントリ
        public class RecentFileEntry
        {
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
        }

        #region コンストラクタ

        public MainWindowViewModel()
        {
            if (IsInDesignMode())
            {
                InitializeDesignMode();
            }
        }

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            ButtonPanelViewModel buttonPanelViewModel,
            ListPanelViewModel listPanelViewModel,
            EditPanelViewModel editPanelViewModel,
            LogPanelViewModel logPanelViewModel,
            FavoritePanelViewModel favoritePanelViewModel,
            System.IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
            
            ButtonPanelViewModel = buttonPanelViewModel ?? throw new ArgumentNullException(nameof(buttonPanelViewModel));
            ListPanelViewModel = listPanelViewModel ?? throw new ArgumentNullException(nameof(listPanelViewModel));
            EditPanelViewModel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
            LogPanelViewModel = logPanelViewModel ?? throw new ArgumentNullException(nameof(logPanelViewModel));
            FavoritePanelViewModel = favoritePanelViewModel ?? throw new ArgumentNullException(nameof(favoritePanelViewModel));

            _logger.LogInformation("MainWindowViewModel: 各ViewModelのDI注入完了");
            
            InitializeRuntime();
        }

        #endregion

        #region 初期化

        private void InitializeDesignMode()
        {
            try
            {
                this.StatusMessage = "デザインモード";
                this.Title = "AutoTool - デザインモード";

                LoadedPlugins.Add(new DesignTimePluginInfo("StandardCommands", "標準コマンドプラグイン", "1.0.0"));
                PluginCount = 1;
                CommandCount = 7;
            }
            catch (Exception ex)
            {
                this.StatusMessage = $"デザインモード初期化エラー: {ex.Message}";
            }
        }

        private void InitializeRuntime()
        {
            try
            {
                _logger?.LogInformation("MainWindowViewModel 初期化開始");
                PrepareViewModels();
                SetupMessaging();
                this.StatusMessage = "初期化完了";
                _logger?.LogInformation("MainWindowViewModel 初期化完了");
            }
            catch (Exception ex)
            {
                var errorMessage = $"初期化エラー: {ex.Message}";
                this.StatusMessage = errorMessage;
                _logger?.LogError(ex, "MainWindowViewModel 初期化中にエラーが発生しました");
            }
        }

        private void PrepareViewModels()
        {
            try
            {
                _logger?.LogDebug("各ViewModelの準備処理を開始します");
                
                ButtonPanelViewModel?.Prepare();
                ListPanelViewModel?.Prepare();
                EditPanelViewModel?.Prepare();
                LogPanelViewModel?.Prepare();
                FavoritePanelViewModel?.Prepare();
                
                _logger?.LogDebug("各ViewModelの準備処理が完了しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ViewModels準備処理中にエラーが発生しました");
            }
        }

        private void SetupMessaging()
        {
            WeakReferenceMessenger.Default.Register<ExitApplicationMessage>(this, (r, m) =>
            {
                Application.Current.Shutdown();
            });

            WeakReferenceMessenger.Default.Register<AppStatusMessage>(this, (r, m) =>
            {
                StatusMessage = m.Message;
            });
            
            WeakReferenceMessenger.Default.Register<RunMessage>(this, async (r, m) =>
            {
                await RunMacroCommand();
            });

            WeakReferenceMessenger.Default.Register<StopMessage>(this, (r, m) =>
            {
                StopMacroCommand();
            });

            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) =>
            {
                LogCommandStart(m);
                UpdateCommandProgress(m.Command);
            });

            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) =>
            {
                LogCommandFinish(m);
                UpdateCommandProgress(m.Command, isFinished: true);
            });

            WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, (r, m) =>
            {
                LogCommandProgress(m);
            });

            WeakReferenceMessenger.Default.Register<CommandErrorMessage>(this, (r, m) =>
            {
                LogCommandError(m);
            });

            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (r, m) =>
            {
                UpdateItemProgress(m);
            });
            
            _logger?.LogDebug("メッセージング設定完了");
        }

        private void SetupPluginEvents()
        {
            if (_pluginService != null)
            {
                _pluginService.PluginLoaded += OnPluginLoaded;
                _pluginService.PluginUnloaded += OnPluginUnloaded;
            }
        }

        private void OnPluginLoaded(object? sender, AutoTool.Services.Plugin.PluginLoadedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePluginInfo();
                this.StatusMessage = $"プラグインが読み込まれました: {e.PluginInfo.Name}";
                _logger?.LogInformation("プラグイン読み込み通知: {PluginName}", e.PluginInfo.Name);
            });
        }

        private void OnPluginUnloaded(object? sender, AutoTool.Services.Plugin.PluginUnloadedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePluginInfo();
                this.StatusMessage = $"プラグインがアンロードされました: {e.PluginId}";
                _logger?.LogInformation("プラグインアンロード通知: {PluginId}", e.PluginId);
            });
        }

        private void UpdatePluginInfo()
        {
            if (_pluginService != null)
            {
                var plugins = _pluginService.GetLoadedPlugins().ToList();
                var commands = _pluginService.GetAvailablePluginCommands().ToList();

                LoadedPlugins.Clear();
                foreach (var plugin in plugins)
                {
                    LoadedPlugins.Add(plugin);
                }

                AvailableCommands.Clear();
                foreach (var command in commands)
                {
                    AvailableCommands.Add(command);
                }

                PluginCount = plugins.Count;
                CommandCount = commands.Count;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (_configurationService != null)
                {
                    WindowWidth = _configurationService.GetValue("WindowWidth", 1200.0);
                    WindowHeight = _configurationService.GetValue("WindowHeight", 800.0);
                    
                    var themeString = _configurationService.GetValue("Theme", "Light");
                    if (Enum.TryParse<AppTheme>(themeString, out var theme))
                    {
                        CurrentTheme = theme;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "設定読み込み中にエラーが発生しました。デフォルト値を使用します。");
            }
        }

        private void ApplyTheme()
        {
            try
            {
                _themeService?.SetTheme(CurrentTheme);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "テーマ適用中にエラーが発生しました: {Theme}", CurrentTheme);
            }
        }

        private void StartPerformanceMonitoring()
        {
            try
            {
                if (_performanceService != null)
                {
                    _performanceService.StartMonitoring();
                    
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    
                    timer.Tick += (s, e) => UpdatePerformanceInfo();
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "パフォーマンス監視開始中にエラーが発生しました");
            }
        }

        private void UpdatePerformanceInfo()
        {
            try
            {
                if (_performanceService != null)
                {
                    var info = _performanceService.GetCurrentInfo();
                    MemoryUsage = $"{info.MemoryUsageMB:F1} MB";
                    CpuUsage = $"{info.CpuUsagePercent:F1}%";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogTrace(ex, "パフォーマンス情報更新中にエラーが発生しました");
            }
        }

        #endregion

        #region マクロ実行機能

        [RelayCommand]
        public async Task RunMacroCommand()
        {
            if (IsRunning)
            {
                _logger?.LogWarning("マクロが既に実行中です");
                return;
            }

            try
            {
                // 実行前の準備
                IsRunning = true;
                CurrentCommandIndex = 0;
                OverallProgress = 0.0;
                StatusMessage = "マクロ実行準備中...";
                SetAllViewModelsRunningState(true);
                
                // 統計初期化
                ExecutionStats = new CommandExecutionStats
                {
                    StartTime = DateTime.Now
                };

                // すべてのアイテムの実行状態をリセット
                ResetAllCommandStates();
                
                _cancellationTokenSource = new CancellationTokenSource();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                LogPanelViewModel?.WriteLog("=== マクロ実行開始 ===");
                _logger?.LogInformation("マクロ実行開始");

                // コマンドリストを取得して検証
                var commandItems = ListPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                var validation = ValidateCommandList(commandItems);

                if (!validation.IsValid)
                {
                    StatusMessage = $"検証エラー: {validation.ErrorMessage}";
                    LogPanelViewModel?.WriteLog($"❌ 検証エラー: {validation.ErrorMessage}");

                    if (!string.IsNullOrEmpty(validation.WarningMessage))
                    {
                        LogPanelViewModel?.WriteLog($"⚠️ 警告: {validation.WarningMessage}");
                    }

                    return;
                }

                if (!string.IsNullOrEmpty(validation.WarningMessage))
                {
                    LogPanelViewModel?.WriteLog($"⚠️ 警告: {validation.WarningMessage}");
                }

                TotalCommands = commandItems.Count(x => x.IsEnable);
                ExecutionStats.TotalCommands = TotalCommands;

                StatusMessage = $"マクロ実行中... ({TotalCommands}コマンド)";

                bool result = false;

                if (_serviceProvider != null && commandItems.Count > 0)
                {
                    try
                    {
                        // MacroFactory に ServiceProvider を設定
                        MacroFactory.SetServiceProvider(_serviceProvider);

                        // プラグインサービス設定（あれば）
                        var pluginService = _serviceProvider.GetService(typeof(AutoTool.Services.Plugin.IPluginService)) as AutoTool.Services.Plugin.IPluginService;
                        if (pluginService != null)
                        {
                            MacroFactory.SetPluginService(pluginService);
                        }

                        LogPanelViewModel?.WriteLog($"🔧 MacroFactoryでマクロコマンドを作成中... ({commandItems.Count}アイテム)");

                        // コマンド階層作成
                        _currentMacroCommand = MacroFactory.CreateMacro(commandItems);

                        LogPanelViewModel?.WriteLog("✅ マクロコマンド作成完了");
                        _logger?.LogInformation("MacroFactoryでマクロコマンドを作成しました");

                        // 実行コンテキストを作成
                        var variableStore = _serviceProvider.GetService(typeof(AutoTool.Command.Interface.IVariableStore)) as AutoTool.Command.Interface.IVariableStore;
                        var executionContext = new CommandExecutionContext(
                            _cancellationTokenSource.Token,
                            variableStore,
                            _serviceProvider);

                        if (_currentMacroCommand is BaseCommand baseCommand)
                        {
                            baseCommand.SetExecutionContext(executionContext);
                        }

                        // 実際のマクロコマンドを実行
                        LogPanelViewModel?.WriteLog("🚀 マクロ実行開始");
                        result = await _currentMacroCommand.Execute(_cancellationTokenSource.Token);

                        stopwatch.Stop();
                        LogPanelViewModel?.WriteLog($"=== マクロ実行完了 ({stopwatch.ElapsedMilliseconds}ms) ===");
                    }
                    catch (OperationCanceledException)
                    {
                        LogPanelViewModel?.WriteLog("=== マクロ実行キャンセル ===");
                        result = false;
                        throw;
                    }
                    catch (FileNotFoundException ex)
                    {
                        _logger?.LogError(ex, "ファイルが見つかりません");
                        LogPanelViewModel?.WriteLog($"❌ ファイルエラー: {ex.Message}");
                        result = false;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        _logger?.LogError(ex, "ディレクトリが見つかりません");
                        LogPanelViewModel?.WriteLog($"❌ ディレクトリエラー: {ex.Message}");
                        result = false;
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger?.LogError(ex, "マクロ構造エラー");
                        LogPanelViewModel?.WriteLog($"❌ マクロ構造エラー: {ex.Message}");
                        result = false;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "マクロ実行エンジンエラー");
                        LogPanelViewModel?.WriteLog($"❌ マクロ実行エンジンエラー: {ex.Message}");
                        result = false;
                    }
                }
                else
                {
                    // ServiceProvider がない場合のダミー実行
                    LogPanelViewModel?.WriteLog("⚠️ ServiceProvider未設定のためダミー実行モード");
                    result = await ExecuteDummyMode(commandItems);
                }

                stopwatch.Stop();

                // 実行統計の更新
                ExecutionStats.EndTime = DateTime.Now;
                ExecutionStats.TotalExecutionTime = stopwatch.Elapsed;

                if (_currentMacroCommand is BaseCommand baseCmd)
                {
                    var stats = baseCmd.ExecutionStats;
                    ExecutionStats.ExecutedCommands = stats.ExecutedCommands;
                    ExecutionStats.SuccessfulCommands = stats.SuccessfulCommands;
                    ExecutionStats.FailedCommands = stats.FailedCommands;
                    ExecutionStats.SkippedCommands = stats.SkippedCommands;
                }
                else
                {
                    ExecutionStats.ExecutedCommands = TotalCommands;
                    ExecutionStats.SuccessfulCommands = result ? TotalCommands : 0;
                    ExecutionStats.FailedCommands = result ? 0 : 1;
                }

                OverallProgress = 100.0;

                if (result)
                {
                    var successRate = ExecutionStats.SuccessRate;
                    StatusMessage = $"マクロ実行完了 ({stopwatch.ElapsedMilliseconds}ms, 成功率: {successRate:F1}%)";
                    LogPanelViewModel?.WriteLog($"✅ 全て成功! 実行時間: {stopwatch.ElapsedMilliseconds}ms");
                    _logger?.LogInformation("マクロ実行完了: 成功率={SuccessRate:F1}%, 時間={Duration}ms", successRate, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    StatusMessage = $"マクロ実行失敗 ({ExecutionStats.FailedCommands}/{ExecutionStats.TotalCommands}個失敗)";
                    LogPanelViewModel?.WriteLog($"❌ 実行失敗: {ExecutionStats.FailedCommands}個のコマンドが失敗");
                    _logger?.LogWarning("マクロ実行失敗: 失敗コマンド={Failed}/{Total}", ExecutionStats.FailedCommands, ExecutionStats.TotalCommands);
                }

                // 実行統計を送信
                WeakReferenceMessenger.Default.Send(new CommandStatsMessage(
                    ExecutionStats.TotalCommands,
                    ExecutionStats.ExecutedCommands,
                    ExecutionStats.SuccessfulCommands,
                    ExecutionStats.FailedCommands,
                    ExecutionStats.TotalExecutionTime));
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "マクロ実行キャンセル";
                LogPanelViewModel?.WriteLog("=== マクロ実行キャンセル ===");
                _logger?.LogInformation("マクロ実行キャンセル");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "マクロ実行中に予期しないエラーが発生しました");
                StatusMessage = $"実行エラー: {ex.Message}";
                LogPanelViewModel?.WriteLog($"❌ 予期しないエラー: {ex.Message}");
                
                // エラーメッセージ送信
                WeakReferenceMessenger.Default.Send(new StatusUpdateMessage("Error", ex.Message));
            }
            finally
            {
                // クリーンアップ
                IsRunning = false;
                SetAllViewModelsRunningState(false);
                CurrentCommandIndex = 0;
                CurrentCommandDescription = string.Empty;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _currentMacroCommand = null;

                // 実行完了後、すべてのアイテムの実行状態をクリア
                ResetAllCommandStates();
            }
        }

        [RelayCommand]
        public void StopMacroCommand()
        {
            try
            {
                if (!IsRunning) return;

                _cancellationTokenSource?.Cancel();
                StatusMessage = "マクロ停止中...";
                _logger?.LogInformation("マクロ停止要求");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "マクロ停止エラー");
                StatusMessage = $"停止エラー: {ex.Message}";
            }
        }

        private async Task<bool> ExecuteDummyMode(List<ICommandListItem> commandItems)
        {
            try
            {
                var enabledItems = commandItems.Where(x => x.IsEnable).ToList();
                
                for (int i = 0; i < enabledItems.Count; i++)
                {
                    if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                        return false;

                    var item = enabledItems[i];
                    CurrentCommandIndex = i + 1;
                    CurrentCommandDescription = $"{item.ItemType} (行 {item.LineNumber})";
                    item.IsRunning = true;

                    try
                    {
                        var dummyCommand = new DummyCommand
                        {
                            LineNumber = item.LineNumber,
                            Description = item.ItemType
                        };
                        
                        WeakReferenceMessenger.Default.Send(new StartCommandMessage(dummyCommand));
                        
                        for (int progress = 0; progress <= 100; progress += 20)
                        {
                            if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                                break;
                            
                            item.Progress = progress;
                            WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(dummyCommand, progress));
                            await Task.Delay(100, _cancellationTokenSource?.Token ?? CancellationToken.None);
                        }
                        
                        OverallProgress = ((double)(i + 1) / enabledItems.Count) * 100.0;
                        
                        WeakReferenceMessenger.Default.Send(new FinishCommandMessage(dummyCommand));
                        
                        LogPanelViewModel?.WriteLog($"✅ 実行完了: {item.ItemType} (行 {item.LineNumber})");
                    }
                    finally
                    {
                        item.IsRunning = false;
                        item.Progress = 100;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ダミー実行モードでエラーが発生しました");
                return false;
            }
        }

        private void ResetAllCommandStates()
        {
            try
            {
                var items = ListPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                foreach (var item in items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }
                _logger?.LogDebug("すべてのコマンドの実行状態をリセットしました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド状態リセット中にエラーが発生しました");
            }
        }

        private void SetAllViewModelsRunningState(bool isRunning)
        {
            try
            {
                ButtonPanelViewModel?.SetRunningState(isRunning);
                ListPanelViewModel?.SetRunningState(isRunning);
                EditPanelViewModel?.SetRunningState(isRunning);
                LogPanelViewModel?.SetRunningState(isRunning);
                FavoritePanelViewModel?.SetRunningState(isRunning);

                _logger?.LogDebug("全ViewModelの実行状態を設定: {IsRunning}", isRunning);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ViewModel実行状態設定中にエラーが発生しました");
            }
        }

        private void LogCommandError(CommandErrorMessage message)
        {
            try
            {
                var command = message.Command;
                var ex = message.Exception;
                
                var errorDetail = ex switch
                {
                    FileNotFoundException => $"ファイルが見つかりません: {ex.Message}",
                    DirectoryNotFoundException => $"ディレクトリが見つかりません: {ex.Message}",
                    TimeoutException => $"タイムアウトしました: {ex.Message}",
                    OperationCanceledException => "操作がキャンセルされました",
                    InvalidOperationException => $"操作が無効です: {ex.Message}",
                    _ => $"予期しないエラー: {ex.Message}"
                };

                LogPanelViewModel?.WriteLog($"❌ [{command.LineNumber:D2}] {command.Description}: {errorDetail}");
                _logger?.LogError(ex, "コマンドエラー: Line={Line}, Description={Description}", 
                    command.LineNumber, command.Description);

                ExecutionStats.FailedCommands++;
                
                UpdateItemErrorState(command.LineNumber);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンドエラーログ出力中にエラーが発生しました");
            }
        }

        private void UpdateItemProgress(UpdateProgressMessage message)
        {
            try
            {
                var items = ListPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                var targetItem = items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                
                if (targetItem != null)
                {
                    targetItem.Progress = message.Progress;
                }

                var enabledItems = items.Where(x => x.IsEnable).ToList();
                if (enabledItems.Count > 0)
                {
                    var totalProgress = enabledItems.Sum(x => x.Progress);
                    OverallProgress = totalProgress / enabledItems.Count;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "進捗更新中にエラーが発生しました");
            }
        }

        private void UpdateCommandProgress(ICommand command, bool isFinished = false)
        {
            try
            {
                CurrentCommandDescription = $"{command.Description} (行 {command.LineNumber})";
                
                var items = ListPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                var targetItem = items.FirstOrDefault(x => x.LineNumber == command.LineNumber);
                
                if (targetItem != null)
                {
                    targetItem.IsRunning = !isFinished;
                    if (isFinished)
                    {
                        targetItem.Progress = 100;
                    }
                }

                if (isFinished)
                {
                    ExecutionStats.ExecutedCommands++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド進捗更新中にエラーが発生しました");
            }
        }

        private void UpdateItemErrorState(int lineNumber)
        {
            try
            {
                var items = ListPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                var targetItem = items.FirstOrDefault(x => x.LineNumber == lineNumber);
                
                if (targetItem != null)
                {
                    targetItem.IsRunning = false;
                    targetItem.Progress = 0; // エラー時は進捗をリセット
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテムエラー状態更新中にエラーが発生しました");
            }
        }

        private void LogCommandStart(StartCommandMessage message)
        {
            try
            {
                var command = message.Command;
                CurrentCommandIndex = command.LineNumber;
                CurrentCommandDescription = command.Description;
                
                var timestamp = message.Timestamp.ToString("HH:mm:ss.fff");
                LogPanelViewModel?.WriteLog($"[{timestamp}][{command.LineNumber:D2}] ▶ {command.Description} 開始");
                _logger?.LogDebug("コマンド開始: Line={Line}, Description={Description}", 
                    command.LineNumber, command.Description);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド開始ログ出力エラー");
            }
        }

        private void LogCommandFinish(FinishCommandMessage message)
        {
            try
            {
                var command = message.Command;
                var timestamp = message.Timestamp.ToString("HH:mm:ss.fff");
                LogPanelViewModel?.WriteLog($"[{timestamp}][{command.LineNumber:D2}] ✓ {command.Description} 完了");
                _logger?.LogDebug("コマンド完了: Line={Line}, Description={Description}", 
                    command.LineNumber, command.Description);
                    
                ExecutionStats.SuccessfulCommands++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド完了ログ出力エラー");
            }
        }

        private void LogCommandProgress(DoingCommandMessage message)
        {
            try
            {
                var command = message.Command;
                var timestamp = message.Timestamp.ToString("HH:mm:ss.fff");
                LogPanelViewModel?.WriteLog($"[{timestamp}][{command.LineNumber:D2}] → {message.Detail}");
                _logger?.LogDebug("コマンド進行: Line={Line}, Detail={Detail}", 
                    command.LineNumber, message.Detail);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド進行ログ出力エラー");
            }
        }

        private ViewModel.Shared.ValidationResult ValidateCommandList(List<ICommandListItem> commandItems)
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                if (commandItems.Count == 0)
                {
                    return new ViewModel.Shared.ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "実行するコマンドがありません",
                        ErrorCount = 1
                    };
                }

                var enabledItems = commandItems.Where(x => x.IsEnable).ToList();
                if (enabledItems.Count == 0)
                {
                    return new ViewModel.Shared.ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "有効なコマンドがありません（すべて無効化されています）",
                        ErrorCount = 1
                    };
                }

                ValidatePairStructure(enabledItems, errors, warnings);
                ValidateRequiredFiles(enabledItems, errors, warnings);
                ValidateCommandSettings(enabledItems, errors, warnings);

                var result = new ViewModel.Shared.ValidationResult
                {
                    IsValid = errors.Count == 0,
                    ErrorMessage = errors.Count > 0 ? string.Join("\n", errors) : string.Empty,
                    WarningMessage = warnings.Count > 0 ? string.Join("\n", warnings) : string.Empty,
                    ErrorCount = errors.Count,
                    WarningCount = warnings.Count,
                    Details = "Pre-run validation"
                };

                if (result.WarningCount > 0)
                {
                    _logger?.LogWarning("検証警告: {WarningCount}件", result.WarningCount);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンドリスト検証中に予期しないエラーが発生しました");
                return new ViewModel.Shared.ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"検証処理エラー: {ex.Message}",
                    ErrorCount = 1
                };
            }
        }

        private void ValidatePairStructure(List<ICommandListItem> items, List<string> errors, List<string> warnings)
        {
            var loopStack = new Stack<ICommandListItem>();
            var ifStack = new Stack<ICommandListItem>();

            foreach (var item in items)
            {
                try
                {
                    switch (item.ItemType)
                    {
                        case "Loop":
                            loopStack.Push(item);
                            if (item is ILoopItem loopItem && loopItem.Pair == null)
                            {
                                errors.Add($"行 {item.LineNumber}: Loop に対応するLoop_Endがありません");
                            }
                            break;

                        case "Loop_End":
                            if (loopStack.Count == 0)
                            {
                                errors.Add($"行 {item.LineNumber}: 対応するLoopがありません");
                            }
                            else
                            {
                                loopStack.Pop();
                            }
                            break;
                        
                        case var type when type.StartsWith("If_") || type.StartsWith("IF_"):
                            if (!type.EndsWith("_End"))
                            {
                                ifStack.Push(item);
                                if (item is IIfItem ifItem && ifItem.Pair == null)
                                {
                                    errors.Add($"行 {item.LineNumber}: {type} に対応するIF_Endがありません");
                                }
                            }
                            break;
                        
                        case "IF_End":
                            if (ifStack.Count == 0)
                            {
                                errors.Add($"行 {item.LineNumber}: 対応するIfがありません");
                            }
                            else
                            {
                                ifStack.Pop();
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"行 {item.LineNumber}: ペア検証中にエラー: {ex.Message}");
                }
            }

            if (loopStack.Count > 0)
            {
                errors.Add($"閉じられていないLoopがあります: {loopStack.Count}個");
            }

            if (ifStack.Count > 0)
            {
                errors.Add($"閉じられていないIfがあります: {ifStack.Count}個");
            }
        }

        private void ValidateRequiredFiles(List<ICommandListItem> items, List<string> errors, List<string> warnings)
        {
            foreach (var item in items)
            {
                try
                {
                    ValidateItemFiles(item, errors, warnings);
                }
                catch (Exception ex)
                {
                    warnings.Add($"行 {item.LineNumber}: ファイル検証中にエラー: {ex.Message}");
                }
            }
        }

        private void ValidateItemFiles(ICommandListItem item, List<string> errors, List<string> warnings)
        {
            var fileProperties = new[]
            {
                ("ImagePath", "画像ファイル"),
                ("ModelPath", "ONNXモデルファイル"), 
                ("ProgramPath", "実行ファイル")
            };

            foreach (var (propName, description) in fileProperties)
            {
                var property = item.GetType().GetProperty(propName);
                if (property?.GetValue(item) is string filePath && !string.IsNullOrEmpty(filePath))
                {
                    var absolutePath = Path.IsPathRooted(filePath) ? 
                        filePath : Path.Combine(Environment.CurrentDirectory, filePath);

                    if (!File.Exists(absolutePath))
                    {
                        errors.Add($"行 {item.LineNumber}: {description}が見つかりません: {filePath}");
                    }
                }
            }

            var dirProperties = new[]
            {
                ("WorkingDirectory", "作業ディレクトリ"),
                ("SaveDirectory", "保存先ディレクトリ")
            };

            foreach (var (propName, description) in dirProperties)
            {
                var property = item.GetType().GetProperty(propName);
                if (property?.GetValue(item) is string dirPath && !string.IsNullOrEmpty(dirPath))
                {
                    var absolutePath = Path.IsPathRooted(dirPath) ? 
                        dirPath : Path.Combine(Environment.CurrentDirectory, dirPath);

                    if (propName == "SaveDirectory")
                    {
                        var parentDir = Path.GetDirectoryName(absolutePath);
                        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                        {
                            warnings.Add($"行 {item.LineNumber}: {description}の親フォルダが見つかりません: {dirPath}");
                        }
                    }
                    else if (!Directory.Exists(absolutePath))
                    {
                        warnings.Add($"行 {item.LineNumber}: {description}が見つかりません: {dirPath}");
                    }
                }
            }
        }

        private void ValidateCommandSettings(List<ICommandListItem> items, List<string> errors, List<string> warnings)
        {
            foreach (var item in items)
            {
                try
                {
                    switch (item.ItemType)
                    {
                        case "Loop":
                            if (item is ILoopItem loopItem && loopItem.LoopCount <= 0)
                            {
                                warnings.Add($"行 {item.LineNumber}: ループ回数が0以下です: {loopItem.LoopCount}");
                            }
                            break;
                            
                        case "Wait":
                            if (item is IWaitItem waitItem && waitItem.Wait <= 0)
                            {
                                warnings.Add($"行 {item.LineNumber}: 待機時間が0以下です: {waitItem.Wait}ms");
                            }
                            break;
                            
                        case "Wait_Image":
                            if (item is IWaitImageItem waitImageItem)
                            {
                                if (waitImageItem.Timeout <= 0)
                                    warnings.Add($"行 {item.LineNumber}: タイムアウト時間が0以下です: {waitImageItem.Timeout}ms");
                                    
                                if (waitImageItem.Threshold < 0 || waitImageItem.Threshold > 1)
                                    warnings.Add($"行 {item.LineNumber}: 閾値が範囲外です: {waitImageItem.Threshold} (0.0-1.0の範囲で設定してください)");
                            }
                            break;
                            
                        case "SetVariable":
                            if (item is ISetVariableItem setVarItem)
                            {
                                var nameProperty = setVarItem.GetType().GetProperty("Name") ?? setVarItem.GetType().GetProperty("VariableName");
                                var name = nameProperty?.GetValue(setVarItem) as string;
                                if (string.IsNullOrEmpty(name))
                                {
                                    errors.Add($"行 {item.LineNumber}: 変数名が設定されていません");
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"行 {item.LineNumber}: 設定値検証中にエラー: {ex.Message}");
                }
            }
        }

        #endregion

        #region コマンド
        
        [RelayCommand]
        private void Undo()
        {
            try
            {
                _logger?.LogDebug("MainWindow: 元に戻すコマンド実行開始");
                WeakReferenceMessenger.Default.Send(new AutoTool.Message.UndoMessage());
                _logger?.LogDebug("MainWindow: Undoメッセージ送信完了");
                this.StatusMessage = "元に戻しました";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "元に戻すコマンド実行中にエラーが発生しました");
                this.StatusMessage = $"元に戻す操作エラー: {ex.Message}";
            }
        }
        
        [RelayCommand]
        private void Redo()
        {
            try
            {
                _logger?.LogDebug("MainWindow: やり直しコマンド実行開始");
                WeakReferenceMessenger.Default.Send(new AutoTool.Message.RedoMessage());
                _logger?.LogDebug("MainWindow: Redoメッセージ送信完了");
                this.StatusMessage = "やり直しました";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "やり直しコマンド実行中にエラーが発生しました");
                this.StatusMessage = $"やり直し操作エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ChangeTheme(string? themeName)
        {
            try
            {
                if (!string.IsNullOrEmpty(themeName) && Enum.TryParse<AppTheme>(themeName, out var theme) && theme != CurrentTheme)
                {
                    CurrentTheme = theme;
                    ApplyTheme();
                    _configurationService?.SetValue("Theme", CurrentTheme.ToString());
                    _logger?.LogInformation("テーマ変更: {Theme}", CurrentTheme);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "テーマ変更中にエラーが発生しました: {Theme}", themeName);
                this.StatusMessage = $"テーマ変更エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ShowAbout()
        {
            try
            {
                var aboutMessage = $"""
                    AutoTool - マクロ自動化ツール
                    
                    バージョン: 1.0.0
                    .NET 8.0
                    
                    読み込み済みプラグイン: {PluginCount}個
                    利用可能なコマンド: {CommandCount}個
                    
                    開発者: AutoTool Development Team
                    """;

                MessageBox.Show(aboutMessage, "AutoToolについて", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Aboutダイアログ表示中にエラーが発生しました");
            }
        }

        [RelayCommand]
        private void RefreshPerformance()
        {
            UpdatePerformanceInfo();
            this.StatusMessage = "パフォーマンス情報を更新しました";
        }

        [RelayCommand]
        private async Task RefreshPlugins()
        {
            try
            {
                IsLoading = true;
                this.StatusMessage = "プラグインを再読み込み中...";

                if (_pluginService != null)
                {
                    await _pluginService.LoadAllPluginsAsync();
                    UpdatePluginInfo();
                    this.StatusMessage = $"プラグイン再読み込み完了: {PluginCount}個のプラグイン";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "プラグイン再読み込み中にエラーが発生しました");
                this.StatusMessage = $"プラグイン再読み込みエラー: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ShowPluginInfo()
        {
            try
            {
                var pluginInfo = "読み込み済みプラグイン:\n\n";
                
                if (LoadedPlugins.Any())
                {
                    foreach (var plugin in LoadedPlugins)
                    {
                        pluginInfo += $"• {plugin.Name} (v{plugin.Version})\n";
                        pluginInfo += $"  ID: {plugin.Id}\n";
                        pluginInfo += $"  説明: {plugin.Description}\n";
                        pluginInfo += $"  作者: {plugin.Author}\n";
                        pluginInfo += $"  状態: {plugin.Status}\n\n";
                    }
                    
                    if (AvailableCommands.Any())
                    {
                        pluginInfo += "利用可能なコマンド:\n\n";
                        var commandsByCategory = AvailableCommands.GroupBy(c => c.Category);
                        foreach (var category in commandsByCategory)
                        {
                            pluginInfo += $"[{category.Key}]\n";
                            foreach (var command in category)
                            {
                                pluginInfo += $"  • {command.Name} ({command.Id})\n";
                                pluginInfo += $"    {command.Description}\n";
                            }
                            pluginInfo += "\n";
                        }
                    }
                }
                else
                {
                    pluginInfo += "読み込み済みのプラグインはありません。";
                }

                MessageBox.Show(pluginInfo, "プラグイン情報", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "プラグイン情報表示中にエラーが発生しました");
            }
        }

        [RelayCommand]
        private async Task LoadPluginFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "プラグインファイルを選択",
                    Filter = "DLLファイル (*.dll)|*.dll|すべてのファイル (*.*)|*.*",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    this.StatusMessage = $"プラグインを読み込み中: {Path.GetFileName(dialog.FileName)}";

                    if (_pluginService != null)
                    {
                        await _pluginService.LoadPluginAsync(dialog.FileName);
                        UpdatePluginInfo();
                        this.StatusMessage = $"プラグイン読み込み完了: {Path.GetFileName(dialog.FileName)}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "プラグインファイル読み込み中にエラーが発生しました");
                this.StatusMessage = $"プラグイン読み込みエラー: {ex.Message}";
                MessageBox.Show($"プラグインの読み込みに失敗しました:\n{ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void OpenFile(string? path = null)
        {
            try
            {
                if (ListPanelViewModel == null) return;

                if (!IsFileOperationEnable)
                {
                    this.StatusMessage = "実行中は開けません";
                    return;
                }

                string? filePath = path;
                if (string.IsNullOrEmpty(filePath))
                {
                    var dlg = new OpenFileDialog
                    {
                        Title = "マクロファイルを開く",
                        Filter = "Macro Files (*.macro)|*.macro|All Files (*.*)|*.*",
                        CheckFileExists = true,
                        Multiselect = false
                    };
                    if (dlg.ShowDialog() == true)
                    {
                        filePath = dlg.FileName;
                    }
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

                ListPanelViewModel.Load(filePath);
                _currentMacroFilePath = filePath;
                IsFileOpened = true;
                AddRecentFile(filePath);
                this.StatusMessage = $"開きました: {Path.GetFileName(filePath)}";
                _logger?.LogInformation("マクロファイルを開きました: {File}", filePath);

                OnPropertyChanged(nameof(MenuItemHeader_SaveFile));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "マクロファイルの読み込みに失敗しました");
                MessageBox.Show($"ファイルを開けませんでした:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SaveFile()
        {
            try
            {
                if (ListPanelViewModel == null) return;
                if (!IsFileOpened || string.IsNullOrEmpty(_currentMacroFilePath))
                {
                    SaveFileAs();
                    return;
                }

                ListPanelViewModel.Save(_currentMacroFilePath);
                this.StatusMessage = $"保存しました: {Path.GetFileName(_currentMacroFilePath)}";
                _logger?.LogInformation("マクロファイルを保存しました: {File}", _currentMacroFilePath);
                AddRecentFile(_currentMacroFilePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "マクロファイルの保存に失敗しました");
                MessageBox.Show($"ファイルを保存できませんでした:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SaveFileAs()
        {
            try
            {
                if (ListPanelViewModel == null) return;

                var dlg = new SaveFileDialog
                {
                    Title = "名前を付けて保存",
                    Filter = "Macro Files (*.macro)|*.macro|All Files (*.*)|*.*",
                    OverwritePrompt = true,
                    AddExtension = true,
                    DefaultExt = ".macro",
                    FileName = string.IsNullOrEmpty(_currentMacroFilePath) ? "macro1.macro" : Path.GetFileName(_currentMacroFilePath)
                };
                if (dlg.ShowDialog() == true)
                {
                    ListPanelViewModel.Save(dlg.FileName);
                    _currentMacroFilePath = dlg.FileName;
                    IsFileOpened = true;
                    AddRecentFile(dlg.FileName);
                    this.StatusMessage = $"保存しました: {Path.GetFileName(dlg.FileName)}";
                    _logger?.LogInformation("マクロファイルを保存しました: {File}", dlg.FileName);
                    OnPropertyChanged(nameof(MenuItemHeader_SaveFile));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "マクロファイルの保存(名前を付けて)に失敗しました");
                MessageBox.Show($"ファイルを保存できませんでした:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Exit()
        {
            try
            {
                _logger?.LogDebug("アプリケーション終了コマンドを実行します");
                SaveWindowSettings();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アプリケーション終了中にエラーが発生しました");
            }
        }

        [RelayCommand]
        private void OpenAppDir()
        {
            try
            {
                var appPath = AppContext.BaseDirectory;
                _logger?.LogDebug("アプリケーションディレクトリを開きます: {Path}", appPath);
                System.Diagnostics.Process.Start("explorer.exe", appPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アプリケーションディレクトリを開く操作に失敗しました");
                this.StatusMessage = $"フォルダを開けませんでした: {ex.Message}";
            }
        }

        #endregion

        #region ヘルパーメソッド

        private void AddRecentFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;
                var existing = RecentFiles.FirstOrDefault(r => string.Equals(r.FilePath, path, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    RecentFiles.Remove(existing);
                }
                RecentFiles.Insert(0, new RecentFileEntry { FileName = Path.GetFileName(path), FilePath = path });

                while (RecentFiles.Count > 10)
                {
                    RecentFiles.RemoveAt(RecentFiles.Count - 1);
                }

                OnPropertyChanged(nameof(RecentFiles));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "最近使ったファイルリスト更新でエラー");
            }
        }

        public void SaveWindowSettings()
        {
            try
            {
                _configurationService?.SetValue("WindowWidth", WindowWidth);
                _configurationService?.SetValue("WindowHeight", WindowHeight);
                _configurationService?.SetValue("WindowState", WindowState.ToString());
                
                _logger?.LogDebug("ウィンドウ設定保存完了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ウィンドウ設定保存中にエラーが発生しました");
            }
        }

        public void Cleanup()
        {
            try
            {
                if (_pluginService != null)
                {
                    _pluginService.PluginLoaded -= OnPluginLoaded;
                    _pluginService.PluginUnloaded -= OnPluginUnloaded;
                }

                _performanceService?.StopMonitoring();
                WeakReferenceMessenger.Default.UnregisterAll(this);
                
                _logger?.LogDebug("MainWindowViewModel クリーンアップ完了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "クリーンアップ中にエラーが発生しました");
            }
        }

        private static bool IsInDesignMode()
            => System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());

        #endregion
    }

    #region 付帯クラス

    /// <summary>
    /// 実行統計
    /// </summary>
    public class CommandExecutionStats
    {
        public int TotalCommands { get; set; }
        public int ExecutedCommands { get; set; }
        public int SuccessfulCommands { get; set; }
        public int FailedCommands { get; set; }
        public int SkippedCommands { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public double SuccessRate => TotalCommands > 0 ? (double)SuccessfulCommands / TotalCommands * 100 : 0;
        public bool IsCompleted => EndTime.HasValue;
    }

    /// <summary>
    /// ダミーコマンド（進捗表示テスト用）
    /// </summary>
    internal class DummyCommand : ICommand
    {
        public int LineNumber { get; set; }
        public bool IsRunning { get; set; }
        public string Description { get; set; } = "ダミーコマンド";
        public ICommand? Parent { get; set; }
        public IEnumerable<ICommand> Children { get; set; } = new List<ICommand>();
        public object? Settings { get; set; }
        public int NestLevel { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;

        public event System.EventHandler? OnStartCommand;

        public Task<bool> Execute(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public void AddChild(ICommand child) { }
        public void RemoveChild(ICommand child) { }
        public IEnumerable<ICommand> GetChildren() => Children;
    }

    public class ExitApplicationMessage
    {
        public string Reason { get; }
        public ExitApplicationMessage(string reason = "") => Reason = reason;
    }

    public class AppStatusMessage
    {
        public string Message { get; }
        public AppStatusMessage(string message) => Message = message;
    }

    public class DesignTimePluginInfo : AutoTool.Services.Plugin.IPluginInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string Description { get; }
        public string Author { get; }
        public DateTime LoadedAt { get; set; }
        public AutoTool.Services.Plugin.PluginStatus Status { get; set; }
        
        public DesignTimePluginInfo(string id, string name, string version)
        {
            Id = id;
            Name = name;
            Version = version;
            Description = "デザインタイム用ダミープラグイン";
            Author = "Design Time";
            LoadedAt = DateTime.Now;
            Status = AutoTool.Services.Plugin.PluginStatus.Active;
        }
    }
    #endregion
}
