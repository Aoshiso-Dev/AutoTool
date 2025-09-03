using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels; // 追加: PanelViewModels用
using AutoTool.Message; // Message
using AutoTool.Services.Plugin; // PluginService
using AutoTool.ViewModel.Shared; // CommandHistoryManager
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.Theme;
using AutoTool.Services.Performance;
using Microsoft.Win32;

namespace AutoTool
{
    /// <summary>
    /// MainWindowViewModel
    /// 各ViewModelを直接DIで受け取るように修正
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        // プライベートフィールド
        private readonly ILogger<MainWindowViewModel>? _logger;
        private readonly IConfigurationService? _configurationService;
        private readonly IThemeService? _themeService;
        private readonly IPerformanceService? _performanceService;
        private readonly AutoTool.Services.Plugin.IPluginService? _pluginService;

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

        // マクロパネルのビューモデル
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

        // ファイル操作が可能か（実行中でない && ViewModel初期化済み）
        public bool IsFileOperationEnable
        {
            get
            {
                if (MacroPanelViewModel == null) return true;
                return !MacroPanelViewModel.IsRunning;
            }
        }

        // IsRunning プロパティの公開（タイプセーフ）
        public bool IsRunning
        {
            get
            {
                return MacroPanelViewModel?.IsRunning ?? false;
            }
        }

        // メニュー表示用ヘッダ
        public string MenuItemHeader_SaveFile => IsFileOpened ? "上書き保存 (_S)" : "保存 (_S)";
        public string MenuItemHeader_SaveFileAs => "名前を付けて保存 (_A)";

        // 最近ファイル用エントリ
        public class RecentFileEntry
        {
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
        }

        // MacroPanelViewModel の状態変更監視（タイプセーフ）
        partial void OnMacroPanelViewModelChanged(AutoTool.ViewModel.MacroPanelViewModel? oldValue, AutoTool.ViewModel.MacroPanelViewModel? newValue)
        {
            if (oldValue != null)
            {
                oldValue.PropertyChanged -= MacroPanel_PropertyChanged;
            }
            if (newValue != null)
            {
                newValue.PropertyChanged += MacroPanel_PropertyChanged;
            }
            OnPropertyChanged(nameof(IsFileOperationEnable));
        }

        private void MacroPanel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AutoTool.ViewModel.MacroPanelViewModel.IsRunning))
            {
                OnPropertyChanged(nameof(IsFileOperationEnable));
                OnPropertyChanged(nameof(IsRunning));
            }
        }

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
        /// 各ViewModelを直接DIで受け取る
        /// </summary>
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            AutoTool.ViewModel.MacroPanelViewModel macroPanelViewModel,
            ButtonPanelViewModel buttonPanelViewModel,
            ListPanelViewModel listPanelViewModel,
            EditPanelViewModel editPanelViewModel,
            LogPanelViewModel logPanelViewModel,
            FavoritePanelViewModel favoritePanelViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 各ViewModelを設定
            MacroPanelViewModel = macroPanelViewModel ?? throw new ArgumentNullException(nameof(macroPanelViewModel));
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

        /// <summary>
        /// デザインモード用の初期化
        /// </summary>
        private void InitializeDesignMode()
        {
            try
            {
                this.StatusMessage = "デザインモード";
                this.Title = "AutoTool - デザインモード";
                
                // デザインモードでは簡単な初期化のみ
                // MacroPanelViewModel = null; // デザインモードでは作成しない

                // デザインモード用のダミープラグイン情報
                LoadedPlugins.Add(new DesignTimePluginInfo("StandardCommands", "標準コマンドプラグイン", "1.0.0"));
                PluginCount = 1;
                CommandCount = 7;
            }
            catch (Exception ex)
            {
                this.StatusMessage = $"デザインモード初期化エラー: {ex.Message}";
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

                // 各ViewModelの準備処理
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

        /// <summary>
        /// 各ViewModelの準備処理
        /// </summary>
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

        /// <summary>
        /// メッセージング設定
        /// </summary>
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

                // 実行中は不可
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

                // ListPanelViewModel経由でファイルを読み込み
                ListPanelViewModel.Load(filePath);
                _currentMacroFilePath = filePath;
                IsFileOpened = true;
                AddRecentFile(filePath);
                this.StatusMessage = $"開きました: {Path.GetFileName(filePath)}";
                _logger?.LogInformation("マクロファイルを開きました: {File}", filePath);

                // メニュー表示を更新
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

                // ListPanelViewModel経由でファイルを保存
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
                    // ListPanelViewModel経由でファイルを保存
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

                // 上限 10 件
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

    #region メッセージクラス
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
    
    // 重複削除（AutoTool.Message.UndoMessage/RedoMessageを使用）
    #endregion

    #region デザインタイム用クラス
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
