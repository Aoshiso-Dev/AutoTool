using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MacroPanels.ViewModel;
using MacroPanels.Message;
using MacroPanels.Plugin;
using MacroPanels.ViewModel.Shared;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.Theme;
using AutoTool.Services.Performance;
using AutoTool.ViewModel; // DI,Pluginブランチの内容を採用

namespace AutoTool
{
    /// <summary>
    /// メインウィンドウのビューモデル
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        // プライベートフィールド
        private readonly ILogger<MainWindowViewModel>? _logger;
        private readonly IConfigurationService? _configurationService;
        private readonly IThemeService? _themeService;
        private readonly IPerformanceService? _performanceService;
        private readonly MacroPanels.Plugin.IPluginService? _pluginService;

        // マクロパネルのビューモデル（DI,Plugin統合版の名前空間を使用）
        [ObservableProperty]
        private AutoTool.ViewModel.MacroPanelViewModel? _macroPanelViewModel;

        // ウィンドウの状態
        [ObservableProperty]
        private double _windowWidth = 1200;

        [ObservableProperty]
        private double _windowHeight = 800;

        [ObservableProperty]
        private WindowState _windowState = WindowState.Normal;

        [ObservableProperty]
        private string _title = "AutoTool - マクロ自動化ツール";

        // ステータス
        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private AppTheme _currentTheme = AppTheme.Light;

        // パフォーマンス情報
        [ObservableProperty]
        private string _memoryUsage = "0 MB";

        [ObservableProperty]
        private string _cpuUsage = "0%";

        // プラグイン情報
        [ObservableProperty]
        private ObservableCollection<IPluginInfo> _loadedPlugins = new();

        [ObservableProperty]
        private ObservableCollection<IPluginCommandInfo> _availableCommands = new();

        [ObservableProperty]
        private int _pluginCount;

        [ObservableProperty]
        private int _commandCount;

        #region コンストラクタ

        /// <summary>
        /// デザイン時用コンストラクタ
        /// </summary>
        public MainWindowViewModel()
        {
            if (IsInDesignMode())
            {
                InitializeDesignMode();
            }
        }

        /// <summary>
        /// 実行時用コンストラクタ
        /// </summary>
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IConfigurationService configurationService,
            IThemeService themeService,
            IPerformanceService performanceService,
            AutoTool.ViewModel.MacroPanelViewModel macroPanelViewModel,
            MacroPanels.Plugin.IPluginService pluginService)
        {
            _logger = logger;
            _configurationService = configurationService;
            _themeService = themeService;
            _performanceService = performanceService;
            _pluginService = pluginService;
            MacroPanelViewModel = macroPanelViewModel;

            InitializeRuntime();
        }

        #endregion

        #region 初期化メソッド

        /// <summary>
        /// デザインモード用の初期化
        /// </summary>
        private void InitializeDesignMode()
        {
            try
            {
                StatusMessage = "デザインモード";
                Title = "AutoTool - デザインモード";
                
                // デザインモード用のダミーデータ
                MacroPanelViewModel = new AutoTool.ViewModel.MacroPanelViewModel(logger: null, null, null, null);

                // デザインモード用のダミープラグイン情報
                LoadedPlugins.Add(new DesignTimePluginInfo("StandardCommands", "標準コマンドプラグイン", "1.0.0"));
                PluginCount = 1;
                CommandCount = 7;
            }
            catch (Exception ex)
            {
                StatusMessage = $"デザインモード初期化エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// 実行時の初期化
        /// </summary>
        private void InitializeRuntime()
        {
            try
            {
                _logger?.LogInformation("MainWindowViewModel 初期化開始");

                // メッセージング設定
                SetupMessaging();

                // プラグインイベントの設定
                SetupPluginEvents();

                // 設定の読み込み
                LoadSettings();

                // テーマの適用
                ApplyTheme();

                // パフォーマンス監視開始
                StartPerformanceMonitoring();

                // プラグイン情報の初期化
                UpdatePluginInfo();

                // CommandHistoryManagerの初期化と設定
                InitializeCommandHistory();

                StatusMessage = "初期化完了";
                _logger?.LogInformation("MainWindowViewModel 初期化完了");
            }
            catch (Exception ex)
            {
                var errorMessage = $"初期化エラー: {ex.Message}";
                StatusMessage = errorMessage;
                _logger?.LogError(ex, "MainWindowViewModel 初期化中にエラーが発生しました");
            }
        }

        /// <summary>
        /// CommandHistoryManagerの初期化と設定
        /// </summary>
        private void InitializeCommandHistory()
        {
            try
            {
                // CommandHistoryManagerを作成してMacroPanelViewModelに設定
                var commandHistory = new CommandHistoryManager();
                MacroPanelViewModel?.SetCommandHistory(commandHistory);
                
                _logger?.LogDebug("CommandHistoryManagerを初期化し、MacroPanelViewModelに設定しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CommandHistoryManager初期化中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// メッセージング設定
        /// </summary>
        private void SetupMessaging()
        {
            // アプリケーション終了メッセージの受信
            WeakReferenceMessenger.Default.Register<ExitApplicationMessage>(this, (r, m) =>
            {
                Application.Current.Shutdown();
            });

            // ステータスメッセージの受信
            WeakReferenceMessenger.Default.Register<StatusMessage>(this, (r, m) =>
            {
                StatusMessage = m.Message;
            });
        }

        /// <summary>
        /// プラグインイベントの設定
        /// </summary>
        private void SetupPluginEvents()
        {
            if (_pluginService != null)
            {
                _pluginService.PluginLoaded += OnPluginLoaded;
                _pluginService.PluginUnloaded += OnPluginUnloaded;
            }
        }

        /// <summary>
        /// プラグイン読み込み時の処理
        /// </summary>
        private void OnPluginLoaded(object? sender, PluginLoadedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePluginInfo();
                StatusMessage = $"プラグインが読み込まれました: {e.PluginInfo.Name}";
                _logger?.LogInformation("プラグイン読み込み通知: {PluginName}", e.PluginInfo.Name);
            });
        }

        /// <summary>
        /// プラグインアンロード時の処理
        /// </summary>
        private void OnPluginUnloaded(object? sender, PluginUnloadedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePluginInfo();
                StatusMessage = $"プラグインがアンロードされました: {e.PluginId}";
                _logger?.LogInformation("プラグインアンロード通知: {PluginId}", e.PluginId);
            });
        }

        /// <summary>
        /// プラグイン情報の更新
        /// </summary>
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

                _logger?.LogDebug("プラグイン情報更新: {PluginCount}個のプラグイン, {CommandCount}個のコマンド", 
                    PluginCount, CommandCount);
            }
        }

        /// <summary>
        /// 設定の読み込み
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (_configurationService != null)
                {
                    // ウィンドウサイズの復元
                    WindowWidth = _configurationService.GetValue("WindowWidth", 1200.0);
                    WindowHeight = _configurationService.GetValue("WindowHeight", 800.0);
                    
                    // テーマ設定の復元
                    var themeString = _configurationService.GetValue("Theme", "Light");
                    if (Enum.TryParse<AppTheme>(themeString, out var theme))
                    {
                        CurrentTheme = theme;
                    }

                    _logger?.LogDebug("設定読み込み完了: Width={Width}, Height={Height}, Theme={Theme}",
                        WindowWidth, WindowHeight, CurrentTheme);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "設定読み込み中にエラーが発生しました。デフォルト値を使用します。");
            }
        }

        /// <summary>
        /// テーマの適用
        /// </summary>
        private void ApplyTheme()
        {
            try
            {
                _themeService?.SetTheme(CurrentTheme);
                _logger?.LogDebug("テーマ適用完了: {Theme}", CurrentTheme);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "テーマ適用中にエラーが発生しました: {Theme}", CurrentTheme);
            }
        }

        /// <summary>
        /// パフォーマンス監視開始
        /// </summary>
        private void StartPerformanceMonitoring()
        {
            try
            {
                if (_performanceService != null)
                {
                    _performanceService.StartMonitoring();
                    
                    // パフォーマンス情報の定期更新
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    
                    timer.Tick += (s, e) => UpdatePerformanceInfo();
                    timer.Start();

                    _logger?.LogDebug("パフォーマンス監視開始");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "パフォーマンス監視開始中にエラーが発生しました");
            }
        }

        /// <summary>
        /// パフォーマンス情報の更新
        /// </summary>
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

        #region コマンド

        [RelayCommand]
        private void ChangeTheme(string? themeName)
        {
            try
            {
                if (!string.IsNullOrEmpty(themeName) && Enum.TryParse<AppTheme>(themeName, out var theme) && theme != CurrentTheme)
                {
                    CurrentTheme = theme;
                    ApplyTheme();
                    
                    // 設定を保存
                    _configurationService?.SetValue("Theme", CurrentTheme.ToString());
                    
                    _logger?.LogInformation("テーマ変更: {Theme}", CurrentTheme);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "テーマ変更中にエラーが発生しました: {Theme}", themeName);
                StatusMessage = $"テーマ変更エラー: {ex.Message}";
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

                _logger?.LogDebug("Aboutダイアログを表示しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Aboutダイアログ表示中にエラーが発生しました");
            }
        }

        [RelayCommand]
        private void RefreshPerformance()
        {
            try
            {
                UpdatePerformanceInfo();
                StatusMessage = "パフォーマンス情報を更新しました";
                _logger?.LogDebug("パフォーマンス情報を手動更新しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "パフォーマンス情報更新中にエラーが発生しました");
                StatusMessage = $"パフォーマンス更新エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RefreshPlugins()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "プラグインを再読み込み中...";

                if (_pluginService != null)
                {
                    await _pluginService.LoadAllPluginsAsync();
                    UpdatePluginInfo();
                    StatusMessage = $"プラグイン再読み込み完了: {PluginCount}個のプラグイン";
                }

                _logger?.LogInformation("プラグイン再読み込み完了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "プラグイン再読み込み中にエラーが発生しました");
                StatusMessage = $"プラグイン再読み込みエラー: {ex.Message}";
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

                _logger?.LogDebug("プラグイン情報を表示しました");
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
                    StatusMessage = $"プラグインを読み込み中: {Path.GetFileName(dialog.FileName)}";

                    if (_pluginService != null)
                    {
                        await _pluginService.LoadPluginAsync(dialog.FileName);
                        UpdatePluginInfo();
                        StatusMessage = $"プラグイン読み込み完了: {Path.GetFileName(dialog.FileName)}";
                    }

                    _logger?.LogInformation("プラグインファイル読み込み完了: {FileName}", dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "プラグインファイル読み込み中にエラーが発生しました");
                StatusMessage = $"プラグイン読み込みエラー: {ex.Message}";
                MessageBox.Show($"プラグインの読み込みに失敗しました:\n{ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region パブリックメソッド

        /// <summary>
        /// ウィンドウ設定を保存
        /// </summary>
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

        /// <summary>
        /// リソースのクリーンアップ
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // プラグインイベントの解除
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

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// デザインモードかどうかを判定
        /// </summary>
        private static bool IsInDesignMode()
        {
            return System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }

        #endregion
    }

    #region メッセージクラス

    /// <summary>
    /// アプリケーション終了メッセージ
    /// </summary>
    public class ExitApplicationMessage
    {
        public string Reason { get; }

        public ExitApplicationMessage(string reason = "")
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// ステータスメッセージ
    /// </summary>
    public class StatusMessage
    {
        public string Message { get; }

        public StatusMessage(string message)
        {
            Message = message;
        }
    }

    #endregion

    #region デザインタイム用クラス

    /// <summary>
    /// デザインタイム用プラグイン情報
    /// </summary>
    public class DesignTimePluginInfo : IPluginInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string Description { get; }
        public string Author { get; }
        public DateTime LoadedAt { get; set; }
        public PluginStatus Status { get; set; }

        public DesignTimePluginInfo(string id, string name, string version)
        {
            Id = id;
            Name = name;
            Version = version;
            Description = "デザインタイム用ダミープラグイン";
            Author = "Design Time";
            LoadedAt = DateTime.Now;
            Status = PluginStatus.Active;
        }
    }

    #endregion
}
