using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.Services;
using AutoTool.Services.Plugin;
using AutoTool.Services.UI;
using AutoTool.ViewModel.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoTool.Command.Interface;
using AutoTool.ViewModel.Panels;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Model.MacroFactory;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// メインウィンドウのViewModel（Service統合版）
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginService;
        private readonly IRecentFileService _recentFileService;
        private readonly IMessenger _messenger;
        private readonly IMainWindowMenuService _menuService;
        private readonly IMainWindowButtonService _buttonService;

        // 基本プロパティ（ObservablePropertyに変更）
        [ObservableProperty]
        private string _title = "AutoTool - 統合マクロ自動化ツール";
        
        [ObservableProperty]
        private double _windowWidth = 1200;
        
        [ObservableProperty]
        private double _windowHeight = 800;
        
        [ObservableProperty]
        private WindowState _windowState = WindowState.Normal;
        
        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private bool _isRunning = false;
        
        [ObservableProperty]
        private string _statusMessage = "準備完了";
        
        [ObservableProperty]
        private string _memoryUsage = "0 MB";
        
        [ObservableProperty]
        private string _cpuUsage = "0%";
        
        [ObservableProperty]
        private int _pluginCount = 0;
        
        [ObservableProperty]
        private int _commandCount = 0;
        
        [ObservableProperty]
        private string _menuItemHeader_SaveFile = "保存(_S)";
        
        [ObservableProperty]
        private string _menuItemHeader_SaveFileAs = "名前を付けて保存(_A)";

        // メニューサービスからRecentFilesを取得
        public ObservableCollection<RecentFileItem> RecentFiles => _menuService?.RecentFiles ?? new();

        // メニューコマンド（MenuServiceから取得）
        public IRelayCommand OpenFileCommand => _menuService?.OpenFileCommand ?? new RelayCommand(() => { });
        public IRelayCommand SaveFileCommand => _menuService?.SaveFileCommand ?? new RelayCommand(() => { });
        public IRelayCommand SaveFileAsCommand => _menuService?.SaveFileAsCommand ?? new RelayCommand(() => { });
        public IRelayCommand ExitCommand => _menuService?.ExitCommand ?? new RelayCommand(() => { });
        public IRelayCommand ChangeThemeCommand => _menuService?.ChangeThemeCommand ?? new RelayCommand<string>(_ => { });
        public IRelayCommand LoadPluginFileCommand => _menuService?.LoadPluginFileCommand ?? new RelayCommand(() => { });
        public IRelayCommand RefreshPluginsCommand => _menuService?.RefreshPluginsCommand ?? new RelayCommand(() => { });
        public IRelayCommand ShowPluginInfoCommand => _menuService?.ShowPluginInfoCommand ?? new RelayCommand(() => { });
        public IRelayCommand OpenAppDirCommand => _menuService?.OpenAppDirCommand ?? new RelayCommand(() => { });
        public IRelayCommand RefreshPerformanceCommand => _menuService?.RefreshPerformanceCommand ?? new RelayCommand(() => { });
        public IRelayCommand ShowAboutCommand => _menuService?.ShowAboutCommand ?? new RelayCommand(() => { });
        public IRelayCommand ClearLogCommand => _menuService?.ClearLogCommand ?? new RelayCommand(() => { });

        // ボタンコマンド（ButtonServiceから取得）
        public IRelayCommand RunMacroCommand => _buttonService?.RunMacroCommand ?? new RelayCommand(() => { });
        public IRelayCommand AddCommandCommand => _buttonService?.AddCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand DeleteCommandCommand => _buttonService?.DeleteCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand UpCommandCommand => _buttonService?.UpCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand DownCommandCommand => _buttonService?.DownCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand ClearCommandCommand => _buttonService?.ClearCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand UndoCommand => _buttonService?.UndoCommand ?? new RelayCommand(() => { });
        public IRelayCommand RedoCommand => _buttonService?.RedoCommand ?? new RelayCommand(() => { });
        public IRelayCommand AddTestCommandCommand => _buttonService?.AddTestCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand TestExecutionHighlightCommand => _buttonService?.TestExecutionHighlightCommand ?? new RelayCommand(() => { });

        // 統合UI関連プロパティ
        [ObservableProperty]
        private ICommandListItem? _selectedItem;
        
        [ObservableProperty]
        private int _selectedLineNumber = -1;
        
        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();
        
        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();
        
        [ObservableProperty]
        private CommandDisplayItem? _selectedItemType;

        // プログレス関連プロパティ
        [ObservableProperty]
        private string _progressText = "";
        
        [ObservableProperty]
        private string _currentExecutingDescription = "";
        
        [ObservableProperty]
        private string _estimatedTimeRemaining = "";

        // 表示制御プロパティ（単純化）
        public bool IsListEmpty => CommandCount == 0;
        public bool IsListNotEmptyButNoSelection => CommandCount > 0 && SelectedItem == null;
        public bool IsNotNullItem => SelectedItem != null;

        /// <summary>
        /// マクロ実行可能かどうか
        /// </summary>
        public bool CanRunMacro => _buttonService?.CanRunMacro ?? false;

        /// <summary>
        /// マクロ停止可能かどうか
        /// </summary>
        public bool CanStopMacro => _buttonService?.CanStopMacro ?? false;

        /// <summary>
        /// DI対応コンストラクタ（Service統合版）
        /// </summary>
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            IRecentFileService recentFileService,
            IPluginService pluginService,
            IMainWindowMenuService menuService,
            IMainWindowButtonService buttonService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _buttonService = buttonService ?? throw new ArgumentNullException(nameof(buttonService));
            _messenger = WeakReferenceMessenger.Default;

            InitializeCommands();
            InitializeProperties();
            InitializeMessaging();
            LoadInitialData();
            SetupMenuServiceEvents();
            SetupButtonServiceEvents();

            _logger.LogInformation("MainWindowViewModel (Service統合版) を初期化しました");
        }

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            try
            {
                // RelayCommandは自動生成されるので、ここでは追加の初期化のみ
                _logger.LogDebug("コマンド初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド初期化中にエラーが発生しました");
            }
        }

        /// <summary>
        /// プロパティの初期化
        /// </summary>
        private void InitializeProperties()
        {
            try
            {
                // 初期値設定
                Title = "AutoTool - 統合マクロ自動化ツール";
                StatusMessage = "準備完了";
                WindowWidth = 1200;
                WindowHeight = 800;
                WindowState = WindowState.Normal;
                
                // サンプルログ追加
                InitializeSampleLog();
                
                _logger.LogDebug("プロパティ初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プロパティ初期化中にエラーが発生しました");
            }
        }

        /// <summary>
        /// Messaging設定
        /// </summary>
        private void InitializeMessaging()
        {
            try
            {
                SetupMessaging();
                _logger.LogDebug("Messaging初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messaging初期化中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 最近開いたファイルの読み込み
        /// </summary>
        private void LoadRecentFiles()
        {
            try
            {
                // IRecentFileServiceから最近開いたファイルを取得
                var recentFiles = _recentFileService.GetRecentFiles();
                
                // MenuServiceのRecentFilesに直接追加
                _menuService.RecentFiles.Clear();
                foreach (var file in recentFiles.Take(10)) // 最大10件
                {
                    _menuService.RecentFiles.Add(new RecentFileItem
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        LastAccessed = DateTime.Now
                    });
                }
                _logger.LogDebug("最近開いたファイル読み込み完了: {Count}件", _menuService.RecentFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近開いたファイル読み込み中にエラーが発生しました");
            }
        }

        /// <summary>
        /// メニューサービスのイベント設定
        /// </summary>
        private void SetupMenuServiceEvents()
        {
            try
            {
                if (_menuService != null)
                {
                    // ファイルオープン・セーブイベントの監視
                    _menuService.FileOpened += (sender, filePath) =>
                    {
                        Title = $"AutoTool - {Path.GetFileName(filePath)}";
                        StatusMessage = $"ファイルを開きました: {Path.GetFileName(filePath)}";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイルオープン: {filePath}");
                    };

                    _menuService.FileSaved += (sender, filePath) =>
                    {
                        Title = $"AutoTool - {Path.GetFileName(filePath)}";
                        StatusMessage = $"ファイルを保存しました: {Path.GetFileName(filePath)}";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイル保存: {filePath}");
                    };
                }

                _logger.LogDebug("MenuService イベント設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MenuService イベント設定中にエラー");
            }
        }

        /// <summary>
        /// ボタンサービスのイベント設定
        /// </summary>
        private void SetupButtonServiceEvents()
        {
            try
            {
                if (_buttonService != null)
                {
                    // 実行状態変更の監視
                    _buttonService.RunningStateChanged += (sender, isRunning) =>
                    {
                        IsRunning = isRunning;
                        OnPropertyChanged(nameof(CanRunMacro));
                        OnPropertyChanged(nameof(CanStopMacro));
                        
                        // ListPanelにも実行状態を通知
                        WeakReferenceMessenger.Default.Send(new MacroExecutionStateMessage(isRunning));
                        
                        _logger.LogDebug("マクロ実行状態変更: {IsRunning}", isRunning);
                    };

                    // ステータス変更の監視
                    _buttonService.StatusChanged += (sender, status) =>
                    {
                        StatusMessage = status;
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {status}");
                    };

                    // コマンド数変更の監視
                    _buttonService.CommandCountChanged += (sender, count) =>
                    {
                        CommandCount = count;
                        OnPropertyChanged(nameof(CanRunMacro));
                        OnPropertyChanged(nameof(CanStopMacro));
                    };
                }

                _logger.LogDebug("ButtonService イベント設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ButtonService イベント設定中にエラー");
            }
        }

        /// <summary>
        /// Messaging設定
        /// </summary>
        private void SetupMessaging()
        {
            try
            {
                // ListPanelからの状態変更メッセージを受信
                _messenger.Register<ChangeSelectedMessage>(this, (r, m) =>
                {
                    SelectedItem = m.SelectedItem;
                    var listPanel = _serviceProvider.GetService<ListPanelViewModel>();
                    if (listPanel != null)
                    {
                        SelectedLineNumber = listPanel.SelectedIndex;
                        CommandCount = listPanel.TotalItems;
                    }
                    UpdateProperties();
                });

                // ListPanelからのアイテム数変更メッセージを受信
                _messenger.Register<ItemCountChangedMessage>(this, (r, m) =>
                {
                    CommandCount = m.Count;
                    _buttonService?.UpdateCommandCount(m.Count); // ButtonServiceにも通知
                    UpdateProperties();
                });

                // ListPanelからのログメッセージを受信
                _messenger.Register<LogMessage>(this, (r, m) =>
                {
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {m.Message}");
                });

                // メニューからのログクリア要求を受信
                _messenger.Register<ClearLogMessage>(this, (r, m) =>
                {
                    LogEntries.Clear();
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ログクリア");
                });

                _logger.LogDebug("Messaging設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messaging設定中にエラーが発生しました");
            }
        }

        private void InitializeItemTypes()
        {
            try
            {
                // CommandRegistryから直接取得
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();
                
                var commandTypes = AutoTool.Model.CommandDefinition.CommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                ItemTypes = new ObservableCollection<CommandDisplayItem>(commandTypes);
                SelectedItemType = ItemTypes.FirstOrDefault();
                _logger.LogDebug("ItemTypes初期化完了: {Count}個", ItemTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemTypes初期化中にエラーが発生しました");
                
                // フォールバック
                ItemTypes = new ObservableCollection<CommandDisplayItem>
                {
                    new CommandDisplayItem { TypeName = "Wait", DisplayName = "待機", Category = "基本" }
                };
                SelectedItemType = ItemTypes.FirstOrDefault();
            }
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(IsListEmpty));
            OnPropertyChanged(nameof(IsListNotEmptyButNoSelection));
            OnPropertyChanged(nameof(IsNotNullItem));
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
        }

        private void InitializeSampleLog()
        {
            try
            {
                LogEntries.Add("[00:00:00] AutoTool Service統合UI初期化完了");
                LogEntries.Add("[00:00:01] 標準MVVM方式に統一");
                LogEntries.Add("[00:00:02] サービス統合パネル表示完了");
                _logger.LogDebug("サンプルログ初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "サンプルログ初期化中にエラーが発生しました");
            }
        }

        partial void OnSelectedLineNumberChanged(int value)
        {
            UpdateProperties();
        }

        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            UpdateProperties();
        }

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
            
            _logger.LogDebug("マクロ実行状態変更: {IsRunning}", value);
        }

        partial void OnCommandCountChanged(int value)
        {
            UpdateProperties();
        }

        partial void OnSelectedItemTypeChanged(CommandDisplayItem? value)
        {
            // ButtonServiceにも選択されたアイテムタイプを通知
            _buttonService?.SetSelectedItemType(value);
        }

        private void LoadInitialData()
        {
            try
            {
                // コマンドタイプの初期化
                InitializeItemTypes();
                
                // 最近開いたファイルを読み込み
                LoadRecentFiles();

                _logger.LogInformation("初期データの読み込みが完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初期データの読み込み中にエラーが発生しました");
            }
        }

        /// <summary>
        /// ウィンドウ設定の保存
        /// </summary>
        public void SaveWindowSettings()
        {
            try
            {
                _logger.LogDebug("ウィンドウ設定保存（未実装）");
                // 今後実装予定
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ設定保存中にエラーが発生しました");
            }
        }

        /// <summary>
        /// クリーンアップ処理
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogDebug("クリーンアップ処理実行");
                // Messagingの登録解除
                _messenger.UnregisterAll(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クリーンアップ処理中にエラーが発生しました");
            }
        }
    }
}