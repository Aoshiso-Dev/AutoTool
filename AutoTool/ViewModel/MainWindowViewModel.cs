using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.Services;
using AutoTool.Services.Plugin;
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
    /// メインウィンドウのViewModel（EditPanel機能統合版）
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginService;
        private readonly IRecentFileService _recentFileService;
        private readonly IMessenger _messenger;
        private readonly EditPanelViewModel _editPanelViewModel;

        // マクロ実行関連
        private CancellationTokenSource? _currentCancellationTokenSource;

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
        
        [ObservableProperty]
        private ObservableCollection<object> _recentFiles = new();

        // 統合UI関連プロパティ（CommandListを使用）
        [ObservableProperty]
        private object _commandList;
        
        [ObservableProperty]
        private ICommandListItem? _selectedItem;
        
        [ObservableProperty]
        private int _selectedLineNumber = -1;
        
        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();
        
        [ObservableProperty]
        private ObservableCollection<CommandTypeInfo> _itemTypes = new();
        
        [ObservableProperty]
        private CommandTypeInfo? _selectedItemType;

        // EditPanel統合プロパティ（EditPanelViewModelから転送）
        [ObservableProperty]
        private string _progressText = "";
        
        [ObservableProperty]
        private string _currentExecutingDescription = "";
        
        [ObservableProperty]
        private string _estimatedTimeRemaining = "";

        // EditPanelViewModelのプロパティをプロキシ
        public ICommandListItem? Item
        {
            get => _editPanelViewModel?.Item;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.Item = value;
                    OnPropertyChanged();
                    UpdateEditPanelProperties();
                }
            }
        }

        // EditPanelの表示制御プロパティ
        public bool IsListEmpty => CommandCount == 0;
        public bool IsListNotEmptyButNoSelection => CommandCount > 0 && SelectedItem == null;
        public bool IsNotNullItem => SelectedItem != null;

        // EditPanelのアイテムタイプ判定プロパティ（プロキシ）
        public bool IsWaitImageItem => _editPanelViewModel?.IsWaitImageItem ?? false;
        public bool IsClickImageItem => _editPanelViewModel?.IsClickImageItem ?? false;
        public bool IsClickImageAIItem => _editPanelViewModel?.IsClickImageAIItem ?? false;
        public bool IsHotkeyItem => _editPanelViewModel?.IsHotkeyItem ?? false;
        public bool IsClickItem => _editPanelViewModel?.IsClickItem ?? false;
        public bool IsWaitItem => _editPanelViewModel?.IsWaitItem ?? false;
        public bool IsLoopItem => _editPanelViewModel?.IsLoopItem ?? false;
        public bool IsLoopEndItem => _editPanelViewModel?.IsLoopEndItem ?? false;
        public bool IsLoopBreakItem => _editPanelViewModel?.IsLoopBreakItem ?? false;
        public bool IsIfImageExistItem => _editPanelViewModel?.IsIfImageExistItem ?? false;
        public bool IsIfImageNotExistItem => _editPanelViewModel?.IsIfImageNotExistItem ?? false;
        public bool IsIfImageExistAIItem => _editPanelViewModel?.IsIfImageExistAIItem ?? false;
        public bool IsIfImageNotExistAIItem => _editPanelViewModel?.IsIfImageNotExistAIItem ?? false;
        public bool IsIfEndItem => _editPanelViewModel?.IsIfEndItem ?? false;
        public bool IsIfVariableItem => _editPanelViewModel?.IsIfVariableItem ?? false;
        public bool IsExecuteItem => _editPanelViewModel?.IsExecuteItem ?? false;
        public bool IsSetVariableItem => _editPanelViewModel?.IsSetVariableItem ?? false;
        public bool IsSetVariableAIItem => _editPanelViewModel?.IsSetVariableAIItem ?? false;
        public bool IsScreenshotItem => _editPanelViewModel?.IsScreenshotItem ?? false;
        
        // 複合条件判定
        public bool IsImageBasedItem => _editPanelViewModel?.IsImageBasedItem ?? false;
        public bool IsAIBasedItem => _editPanelViewModel?.IsAIBasedItem ?? false;
        public bool IsVariableItem => _editPanelViewModel?.IsVariableItem ?? false;
        public bool IsLoopRelatedItem => _editPanelViewModel?.IsLoopRelatedItem ?? false;
        public bool IsIfRelatedItem => _editPanelViewModel?.IsIfRelatedItem ?? false;
        
        // 表示制御プロパティ
        public bool ShowWindowInfo => _editPanelViewModel?.ShowWindowInfo ?? false;
        public bool ShowAdvancedSettings => _editPanelViewModel?.ShowAdvancedSettings ?? false;

        // EditPanelViewModelの基本設定プロパティ（プロキシ）
        public string Comment
        {
            get => _editPanelViewModel?.Comment ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.Comment = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WindowTitle
        {
            get => _editPanelViewModel?.WindowTitle ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WindowTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WindowClassName
        {
            get => _editPanelViewModel?.WindowClassName ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WindowClassName = value;
                    OnPropertyChanged();
                }
            }
        }

        // 画像関連プロパティ（プロキシ）
        public string ImagePath
        {
            get => _editPanelViewModel?.ImagePath ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ImagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Threshold
        {
            get => _editPanelViewModel?.Threshold ?? 0.8;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.Threshold = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color? SearchColor
        {
            get => _editPanelViewModel?.SearchColor;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.SearchColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Timeout
        {
            get => _editPanelViewModel?.Timeout ?? 5000;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.Timeout = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Interval
        {
            get => _editPanelViewModel?.Interval ?? 500;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.Interval = value;
                    OnPropertyChanged();
                }
            }
        }

        // クリック関連プロパティ（プロキシ）
        public MouseButton MouseButton
        {
            get => _editPanelViewModel?.MouseButton ?? MouseButton.Left;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.MouseButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ClickX
        {
            get => _editPanelViewModel?.ClickX ?? 0;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ClickX = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ClickY
        {
            get => _editPanelViewModel?.ClickY ?? 0;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ClickY = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UseBackgroundClick
        {
            get => _editPanelViewModel?.UseBackgroundClick ?? false;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.UseBackgroundClick = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BackgroundClickMethod
        {
            get => _editPanelViewModel?.BackgroundClickMethod ?? 0;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.BackgroundClickMethod = value;
                    OnPropertyChanged();
                }
            }
        }

        // ホットキー関連プロパティ（プロキシ）
        public bool CtrlKey
        {
            get => _editPanelViewModel?.CtrlKey ?? false;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.CtrlKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AltKey
        {
            get => _editPanelViewModel?.AltKey ?? false;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.AltKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShiftKey
        {
            get => _editPanelViewModel?.ShiftKey ?? false;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ShiftKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public Key SelectedKey
        {
            get => _editPanelViewModel?.SelectedKey ?? Key.Escape;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.SelectedKey = value;
                    OnPropertyChanged();
                }
            }
        }

        // 待機関連プロパティ（プロキシ）
        public int WaitHours
        {
            get => _editPanelViewModel?.WaitHours ?? 0;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WaitHours = value;
                    OnPropertyChanged();
                }
            }
        }

        public int WaitMinutes
        {
            get => _editPanelViewModel?.WaitMinutes ?? 0;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WaitMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

        public int WaitSeconds
        {
            get => _editPanelViewModel?.WaitSeconds ?? 1;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WaitSeconds = value;
                    OnPropertyChanged();
                }
            }
        }

        public int WaitMilliseconds
        {
            get => _editPanelViewModel?.WaitMilliseconds ?? 0;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WaitMilliseconds = value;
                    OnPropertyChanged();
                }
            }
        }

        // ループ関連プロパティ（プロキシ）
        public int LoopCount
        {
            get => _editPanelViewModel?.LoopCount ?? 1;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.LoopCount = value;
                    OnPropertyChanged();
                }
            }
        }

        // 変数関連プロパティ（プロキシ）
        public string VariableName
        {
            get => _editPanelViewModel?.VariableName ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.VariableName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VariableValue
        {
            get => _editPanelViewModel?.VariableValue ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.VariableValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VariableOperator
        {
            get => _editPanelViewModel?.VariableOperator ?? "==";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.VariableOperator = value;
                    OnPropertyChanged();
                }
            }
        }

        // AI関連プロパティ（プロキシ）
        public string ModelPath
        {
            get => _editPanelViewModel?.ModelPath ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ModelPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ClassID
        {
            get => _editPanelViewModel?.ClassID ?? 0;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ClassID = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ConfThreshold
        {
            get => _editPanelViewModel?.ConfThreshold ?? 0.5;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ConfThreshold = value;
                    OnPropertyChanged();
                }
            }
        }

        public double IoUThreshold
        {
            get => _editPanelViewModel?.IoUThreshold ?? 0.25;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.IoUThreshold = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AiDetectMode
        {
            get => _editPanelViewModel?.AiDetectMode ?? "Class";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.AiDetectMode = value;
                    OnPropertyChanged();
                }
            }
        }

        // プログラム実行関連プロパティ（プロキシ）
        public string ProgramPath
        {
            get => _editPanelViewModel?.ProgramPath ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.ProgramPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Arguments
        {
            get => _editPanelViewModel?.Arguments ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.Arguments = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WorkingDirectory
        {
            get => _editPanelViewModel?.WorkingDirectory ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WorkingDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool WaitForExit
        {
            get => _editPanelViewModel?.WaitForExit ?? false;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.WaitForExit = value;
                    OnPropertyChanged();
                }
            }
        }

        // スクリーンショット関連プロパティ（プロキシ）
        public string SaveDirectory
        {
            get => _editPanelViewModel?.SaveDirectory ?? "";
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.SaveDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        // EditPanelViewModelのコレクション（プロキシ）
        public ObservableCollection<MouseButton> MouseButtons => _editPanelViewModel?.MouseButtons ?? new();
        public ObservableCollection<Key> KeyList => _editPanelViewModel?.KeyList ?? new();
        public ObservableCollection<OperatorItem> Operators => _editPanelViewModel?.Operators ?? new();
        public ObservableCollection<AIDetectModeItem> AiDetectModes => _editPanelViewModel?.AiDetectModes ?? new();
        public ObservableCollection<Shared.BackgroundClickMethodItem> BackgroundClickMethods => _editPanelViewModel?.BackgroundClickMethods ?? new();

        // EditPanel統合のための追加プロパティ
        public CommandDisplayItem? SelectedItemTypeObj
        {
            get => _editPanelViewModel?.SelectedItemTypeObj;
            set
            {
                if (_editPanelViewModel != null)
                {
                    _editPanelViewModel.SelectedItemTypeObj = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// マクロ実行可能かどうか
        /// </summary>
        public bool CanRunMacro => !IsRunning && CommandCount > 0;

        /// <summary>
        /// マクロ停止可能かどうか
        /// </summary>
        public bool CanStopMacro => IsRunning;

        /// <summary>
        /// コマンドタイプ情報クラス
        /// </summary>
        public class CommandTypeInfo
        {
            public string TypeName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public Type? ItemType { get; set; }
        }

        /// <summary>
        /// DI対応コンストラクタ
        /// </summary>
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            IRecentFileService recentFileService,
            IPluginService pluginService,
            EditPanelViewModel editPanelViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _editPanelViewModel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
            _messenger = WeakReferenceMessenger.Default;

            _commandList = _serviceProvider.GetService<object>() ?? new object();

            InitializeCommands();
            InitializeProperties();
            InitializeMessaging();
            LoadInitialData();
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
                SetupRunStopMessaging();
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
                RecentFiles.Clear();
                foreach (var file in recentFiles.Take(10)) // 最大10件
                {
                    RecentFiles.Add(new { FileName = Path.GetFileName(file), FilePath = file });
                }
                _logger.LogDebug("最近開いたファイル読み込み完了: {Count}件", RecentFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近開いたファイル読み込み中にエラーが発生しました");
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
                    Item = m.SelectedItem; // EditPanelViewModelにも設定
                    var listPanel = _serviceProvider.GetService<ListPanelViewModel>();
                    if (listPanel != null)
                    {
                        SelectedLineNumber = listPanel.SelectedIndex;
                        CommandCount = listPanel.TotalItems;
                    }
                    UpdateProperties();
                    UpdateEditPanelProperties();
                });

                // ListPanelからのアイテム数変更メッセージを受信
                _messenger.Register<ItemCountChangedMessage>(this, (r, m) =>
                {
                    CommandCount = m.Count;
                    UpdateProperties();
                });

                // ListPanelからのログメッセージを受信
                _messenger.Register<LogMessage>(this, (r, m) =>
                {
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {m.Message}");
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
                // EditPanelViewModelのItemTypesを使用
                if (_editPanelViewModel?.ItemTypes != null)
                {
                    var commandTypes = _editPanelViewModel.ItemTypes
                        .Select(item => new CommandTypeInfo
                        {
                            TypeName = item.TypeName,
                            DisplayName = item.DisplayName,
                            Category = item.Category,
                            ItemType = typeof(BasicCommandItem)
                        })
                        .ToList();

                    ItemTypes = new ObservableCollection<CommandTypeInfo>(commandTypes);
                    SelectedItemType = ItemTypes.FirstOrDefault();
                    _logger.LogDebug("ItemTypes初期化完了（EditPanelから）: {Count}個", ItemTypes.Count);
                }
                else
                {
                    // フォールバック
                    var commandTypes = new List<CommandTypeInfo>
                    {
                        new CommandTypeInfo { TypeName = "Wait", DisplayName = "待機", Category = "基本", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "Click", DisplayName = "クリック", Category = "基本", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "Wait_Image", DisplayName = "画像待機", Category = "画像", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "Click_Image", DisplayName = "画像クリック", Category = "画像", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "Hotkey", DisplayName = "ホットキー", Category = "入力", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "Loop", DisplayName = "ループ開始", Category = "制御", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "Loop_End", DisplayName = "ループ終了", Category = "制御", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "IF_ImageExist", DisplayName = "画像存在判定", Category = "条件", ItemType = typeof(BasicCommandItem) },
                        new CommandTypeInfo { TypeName = "IF_End", DisplayName = "条件終了", Category = "条件", ItemType = typeof(BasicCommandItem) }
                    };

                    ItemTypes = new ObservableCollection<CommandTypeInfo>(commandTypes);
                    SelectedItemType = ItemTypes.FirstOrDefault();
                    _logger.LogDebug("ItemTypes初期化完了（フォールバック）: {Count}個", ItemTypes.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemTypes初期化中にエラーが発生しました");
                
                // フォールバック
                ItemTypes = new ObservableCollection<CommandTypeInfo>
                {
                    new CommandTypeInfo { TypeName = "Wait", DisplayName = "待機", Category = "基本", ItemType = typeof(object) }
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

        /// <summary>
        /// EditPanelプロパティを更新
        /// </summary>
        private void UpdateEditPanelProperties()
        {
            // 判定系
            OnPropertyChanged(nameof(IsWaitImageItem));
            OnPropertyChanged(nameof(IsClickImageItem));
            OnPropertyChanged(nameof(IsClickImageAIItem));
            OnPropertyChanged(nameof(IsHotkeyItem));
            OnPropertyChanged(nameof(IsClickItem));
            OnPropertyChanged(nameof(IsWaitItem));
            OnPropertyChanged(nameof(IsLoopItem));
            OnPropertyChanged(nameof(IsLoopEndItem));
            OnPropertyChanged(nameof(IsLoopBreakItem));
            OnPropertyChanged(nameof(IsIfImageExistItem));
            OnPropertyChanged(nameof(IsIfImageNotExistItem));
            OnPropertyChanged(nameof(IsIfImageExistAIItem));
            OnPropertyChanged(nameof(IsIfImageNotExistAIItem));
            OnPropertyChanged(nameof(IsIfEndItem));
            OnPropertyChanged(nameof(IsIfVariableItem));
            OnPropertyChanged(nameof(IsExecuteItem));
            OnPropertyChanged(nameof(IsSetVariableItem));
            OnPropertyChanged(nameof(IsSetVariableAIItem));
            OnPropertyChanged(nameof(IsScreenshotItem));
            
            // 複合条件
            OnPropertyChanged(nameof(IsImageBasedItem));
            OnPropertyChanged(nameof(IsAIBasedItem));
            OnPropertyChanged(nameof(IsVariableItem));
            OnPropertyChanged(nameof(IsLoopRelatedItem));
            OnPropertyChanged(nameof(IsIfRelatedItem));
            
            // 表示制御
            OnPropertyChanged(nameof(ShowWindowInfo));
            OnPropertyChanged(nameof(ShowAdvancedSettings));

            // 値プロパティ（UIに表示される数値/テキスト類）
            OnPropertyChanged(nameof(Comment));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(WindowClassName));
            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(Threshold));
            OnPropertyChanged(nameof(SearchColor));
            OnPropertyChanged(nameof(Timeout));
            OnPropertyChanged(nameof(Interval));
            OnPropertyChanged(nameof(MouseButton));
            OnPropertyChanged(nameof(ClickX));
            OnPropertyChanged(nameof(ClickY));
            OnPropertyChanged(nameof(UseBackgroundClick));
            OnPropertyChanged(nameof(BackgroundClickMethod));
            OnPropertyChanged(nameof(CtrlKey));
            OnPropertyChanged(nameof(AltKey));
            OnPropertyChanged(nameof(ShiftKey));
            OnPropertyChanged(nameof(SelectedKey));
            OnPropertyChanged(nameof(WaitHours));
            OnPropertyChanged(nameof(WaitMinutes));
            OnPropertyChanged(nameof(WaitSeconds));
            OnPropertyChanged(nameof(WaitMilliseconds));
            OnPropertyChanged(nameof(LoopCount));
            OnPropertyChanged(nameof(VariableName));
            OnPropertyChanged(nameof(VariableValue));
            OnPropertyChanged(nameof(VariableOperator));
            OnPropertyChanged(nameof(ModelPath));
            OnPropertyChanged(nameof(ClassID));
            OnPropertyChanged(nameof(ConfThreshold));
            OnPropertyChanged(nameof(IoUThreshold));
            OnPropertyChanged(nameof(AiDetectMode));
            OnPropertyChanged(nameof(ProgramPath));
            OnPropertyChanged(nameof(Arguments));
            OnPropertyChanged(nameof(WorkingDirectory));
            OnPropertyChanged(nameof(WaitForExit));
            OnPropertyChanged(nameof(SaveDirectory));

            // コレクション（必要に応じて）
            OnPropertyChanged(nameof(MouseButtons));
            OnPropertyChanged(nameof(KeyList));
            OnPropertyChanged(nameof(Operators));
            OnPropertyChanged(nameof(AiDetectModes));
            OnPropertyChanged(nameof(BackgroundClickMethods));

            // アイテムタイプ選択
            OnPropertyChanged(nameof(SelectedItemTypeObj));

            _logger.LogDebug("EditPanelプロパティを更新しました");
        }

        private void InitializeSampleLog()
        {
            try
            {
                LogEntries.Add("[00:00:00] AutoTool DI + Messaging統合UI初期化完了");
                LogEntries.Add("[00:00:01] コマンドシステム準備完了");
                LogEntries.Add("[00:00:02] 統合パネルUI表示完了");
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
            Item = value; // EditPanelViewModelにも設定
            UpdateProperties();
            UpdateEditPanelProperties();
        }

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
            
            // EditPanelViewModelにも実行状態を設定
            if (_editPanelViewModel != null)
            {
                _editPanelViewModel.IsRunning = value;
            }
            
            _logger.LogDebug("マクロ実行状態変更: {IsRunning}", value);
        }

        partial void OnCommandCountChanged(int value)
        {
            UpdateProperties();
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
        /// テスト用のダミーコマンドクラス
        /// </summary>
        private class TestCommand : AutoTool.Command.Interface.ICommand
        {
            public int LineNumber { get; set; }
            public bool IsEnabled { get; set; } = true;
            public AutoTool.Command.Interface.ICommand? Parent { get; set; }
            public IEnumerable<AutoTool.Command.Interface.ICommand> Children { get; set; } = new List<AutoTool.Command.Interface.ICommand>();
            public int NestLevel { get; set; }
            public object? Settings { get; set; }
            public string Description { get; set; } = "テストコマンド";

            // イベント（必要に応じて実装）
            public event EventHandler? OnStartCommand;
            public event EventHandler? OnFinishCommand;

            public void AddChild(AutoTool.Command.Interface.ICommand child) { }
            public void RemoveChild(AutoTool.Command.Interface.ICommand child) { }
            public IEnumerable<AutoTool.Command.Interface.ICommand> GetChildren() => Children;
            public Task<bool> Execute(System.Threading.CancellationToken cancellationToken) => Task.FromResult(true);
        }

        private async Task PrepareForExecution()
        {
            try
            {
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                if (listPanelViewModel != null)
                {
                    // 実行前の準備（必要に応じて実装）
                    listPanelViewModel.InitializeProgress();
                }
                
                _logger.LogDebug("実行準備完了");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行準備中にエラーが発生しました");
            }
        }

        private async Task CleanupAfterExecution()
        {
            try
            {
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                if (listPanelViewModel != null)
                {
                    // listPanelViewModel.SetRunningState(false); // 一時的にコメントアウト
                }
                
                _logger.LogDebug("実行後クリーンアップ完了");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行後クリーンアップ中にエラーが発生しました");
            }
        }

        [RelayCommand]
        public void AddTestCommand()
        {
            try
            {
                // Messagingを使用してテストコマンド追加
                _messenger.Send(new AddMessage("Wait"));
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] テストコマンド追加要求");
                _logger.LogDebug("テストコマンド追加要求送信");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テストコマンド追加中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: テストコマンド追加失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void TestExecutionHighlight()
        {
            try
            {
                // DIからListPanelViewModelを取得
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                if (listPanelViewModel == null)
                {
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ListPanelViewModelが見つかりません");
                    return;
                }

                if (listPanelViewModel.Items.Count == 0)
                {
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: テスト対象のコマンドがありません");
                    return;
                }

                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] 実行ハイライトテスト開始");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] 検出されたアイテム数: {listPanelViewModel.Items.Count}");

                // 各アイテムの詳細をログ出力
                foreach (var item in listPanelViewModel.Items.Take(5))
                {
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] アイテム: Line{item.LineNumber}, Type={item.ItemType}, Enable={item.IsEnable}");
                }

                // 最初のアイテムを実行中状態にする
                var firstItem = listPanelViewModel.Items.First();
                
                // UIスレッドで実行状態を設定
                Application.Current.Dispatcher.Invoke(() =>
                {
                    firstItem.IsRunning = true;
                    firstItem.Progress = 0;
                    listPanelViewModel.CurrentExecutingItem = firstItem;
                    
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] 実行状態設定完了: {firstItem.ItemType} (行{firstItem.LineNumber})");
                    
                    // 手動でメッセージを送信してテスト
                    var testStartMessage = new StartCommandMessage(new TestCommand { LineNumber = firstItem.LineNumber });
                    WeakReferenceMessenger.Default.Send(testStartMessage);
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] StartCommandMessage送信: Line{firstItem.LineNumber}");
                });

                // プログレスを段階的に更新
                Task.Run(async () =>
                {
                    for (int i = 0; i <= 100; i += 10)
                    {
                        await Task.Delay(500); // 500msごとに更新
                        
                        // UIスレッドで実行
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            firstItem.Progress = i;
                            
                            // 手動で進捗メッセージを送信
                            var testProgressMessage = new UpdateProgressMessage(new TestCommand { LineNumber = firstItem.LineNumber }, i);
                            WeakReferenceMessenger.Default.Send(testProgressMessage);
                            
                            LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] テスト進捗: {i}% (アイテム: {firstItem.ItemType})");
                        });
                    }

                    // 完了状態に設定
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        firstItem.IsRunning = false;
                        firstItem.Progress = 100;
                        listPanelViewModel.CurrentExecutingItem = null;
                        
                        // 完了メッセージを送信
                        var testFinishMessage = new FinishCommandMessage(new TestCommand { LineNumber = firstItem.LineNumber });
                        WeakReferenceMessenger.Default.Send(testFinishMessage);
                        
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] 実行ハイライトテスト完了");

                        // 少し待ってからプログレスをリセット
                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                firstItem.Progress = 0;
                                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] プログレスリセット完了");
                            });
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行ハイライトテスト中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: 実行ハイライトテスト失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        private void AddCommand()
        {
            try
            {
                if (SelectedItemType != null)
                {
                    _logger.LogDebug("追加要求: {Type}", SelectedItemType.TypeName);
                    _messenger.Send(new AddMessage(SelectedItemType.TypeName));
                }
                else
                {
                    _logger.LogWarning("追加要求: SelectedItemType が null です");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCommand 実行中にエラー");
            }
        }

        [RelayCommand]
        private void DeleteCommand()
        {
            try
            {
                _logger.LogDebug("削除要求を送信");
                _messenger.Send(new DeleteMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCommand 実行中にエラー");
            }
        }

        [RelayCommand]
        private void UpCommand()
        {
            try
            {
                _logger.LogDebug("上移動要求を送信");
                _messenger.Send(new UpMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpCommand 実行中にエラー");
            }
        }

        [RelayCommand]
        private void DownCommand()
        {
            try
            {
                _logger.LogDebug("下移動要求を送信");
                _messenger.Send(new DownMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownCommand 実行中にエラー");
            }
        }

        [RelayCommand]
        private void ClearCommand()
        {
            try
            {
                _logger.LogDebug("クリア要求を送信");
                _messenger.Send(new ClearMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCommand 実行中にエラー");
            }
        }

        [RelayCommand]
        private void UndoCommand()
        {
            try
            {
                _logger.LogDebug("Undo要求を送信");
                _messenger.Send(new UndoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UndoCommand 実行中にエラー");
            }
        }

        [RelayCommand]
        private void RedoCommand()
        {
            try
            {
                _logger.LogDebug("Redo要求を送信");
                _messenger.Send(new RedoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RedoCommand 実行中にエラー");
            }
        }

        [RelayCommand]
        private void RunMacro()
        {
            try
            {
                if (IsRunning)
                {
                    _logger.LogInformation("停止要求を送信します");
                    _messenger.Send(new StopMessage());
                    StatusMessage = "停止要求を送信しました";
                }
                else
                {
                    _logger.LogInformation("実行要求を送信します");
                    _messenger.Send(new RunMessage());
                    StatusMessage = "実行要求を送信しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacroCommand 実行中にエラー");
                StatusMessage = $"実行エラー: {ex.Message}";
            }
        }

        private async Task StartMacroAsync()
        {
            try
            {
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                if (listPanelViewModel == null)
                {
                    _logger.LogError("ListPanelViewModel が解決できません。実行を中止します。");
                    StatusMessage = "実行エラー: ListPanel VM 未解決";
                    return;
                }

                if (IsRunning)
                {
                    _logger.LogWarning("既に実行中のため開始しません");
                    return;
                }
                if (listPanelViewModel.Items.Count == 0)
                {
                    _logger.LogWarning("実行対象コマンドがありません");
                    StatusMessage = "実行対象がありません";
                    return;
                }

                // 準備
                IsRunning = true;
                listPanelViewModel.SetRunningState(true);
                listPanelViewModel.InitializeProgress();
                _currentCancellationTokenSource = new CancellationTokenSource();
                var token = _currentCancellationTokenSource.Token;

                // MacroFactory にサービスを渡す
                MacroFactory.SetServiceProvider(_serviceProvider);
                if (_pluginService != null)
                {
                    MacroFactory.SetPluginService(_pluginService);
                }

                // スナップショットを作成
                var itemsSnapshot = listPanelViewModel.Items.ToList();

                await Task.Run(async () =>
                {
                    try
                    {
                        // マクロを生成して実行
                        var root = MacroFactory.CreateMacro(itemsSnapshot);
                        var result = await root.Execute(token);
                        _logger.LogInformation("マクロ実行完了: {Result}", result);
                        StatusMessage = result ? "実行完了" : "一部失敗/中断";
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("マクロがキャンセルされました");
                        StatusMessage = "実行キャンセル";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "マクロ実行中にエラー");
                        StatusMessage = $"実行エラー: {ex.Message}";
                    }
                    finally
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            listPanelViewModel.SetRunningState(false);
                            listPanelViewModel.CompleteProgress();
                            IsRunning = false;
                            _currentCancellationTokenSource?.Dispose();
                            _currentCancellationTokenSource = null;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartMacroAsync 内でエラー");
                StatusMessage = $"実行エラー: {ex.Message}";
                IsRunning = false;
            }
        }

        private void StopMacroInternal()
        {
            try
            {
                if (_currentCancellationTokenSource != null && ! _currentCancellationTokenSource.IsCancellationRequested)
                {
                    _currentCancellationTokenSource.Cancel();
                    _logger.LogInformation("キャンセル要求を送信しました");
                    StatusMessage = "停止要求中...";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止処理中にエラー");
            }
        }

        private void SetupRunStopMessaging()
        {
            _messenger.Register<RunMessage>(this, (r, m) => { _ = StartMacroAsync(); });
            _messenger.Register<StopMessage>(this, (r, m) => { StopMacroInternal(); });
        }
    }
}
