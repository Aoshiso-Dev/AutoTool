using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.Services;
using AutoTool.Services.Plugin;
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

namespace AutoTool.ViewModel
{
    /// <summary>
    /// メインウィンドウのViewModel（DI + Messaging統合版）
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginService; // 正しい型に修正
        private readonly IRecentFileService _recentFileService;
        private readonly IMessenger _messenger;

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
        private object _commandList; // CommandList の一時的な代替
        
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

        /// <summary>
        /// リストが空かどうか
        /// </summary>
        public bool IsListEmpty => CommandCount == 0;

        /// <summary>
        /// リストが空ではないが選択されていないかどうか
        /// </summary>
        public bool IsListNotEmptyButNoSelection => CommandCount > 0 && SelectedItem == null;

        /// <summary>
        /// アイテムが選択されているかどうか
        /// </summary>
        public bool IsNotNullItem => SelectedItem != null;

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
            IPluginService pluginService) // 正しい型に修正
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _messenger = WeakReferenceMessenger.Default;

            _commandList = _serviceProvider.GetService<object>() ?? new object(); // CommandListの一時的な代替

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
                // 基本的なコマンドタイプを手動で追加
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
                _logger.LogDebug("ItemTypes初期化完了: {Count}個", ItemTypes.Count);
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

        // コマンド実装（Messaging使用）
        [RelayCommand]
        public void AddCommand()
        {
            try
            {
                _logger.LogDebug("AddCommand開始 - SelectedItemType: {SelectedItemType}", SelectedItemType?.TypeName ?? "null");
                
                if (SelectedItemType != null)
                {
                    // Messagingを使用してListPanelにコマンド追加要求を送信
                    _messenger.Send(new AddMessage(SelectedItemType.TypeName));
                    
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド追加要求: {SelectedItemType.DisplayName}");
                    _logger.LogDebug("AddMessageを送信: {TypeName}", SelectedItemType.TypeName);
                }
                else
                {
                    _logger.LogWarning("SelectedItemTypeがnullです");
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンドタイプが選択されていません");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド追加中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド追加失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void DeleteCommand()
        {
            try
            {
                // Messagingを使用してListPanelに削除要求を送信
                _messenger.Send(new DeleteMessage());
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド削除要求");
                _logger.LogDebug("DeleteMessageを送信");
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド削除中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド削除失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void UpCommand()
        {
            try
            {
                // Messagingを使用してListPanelに上移動要求を送信
                _messenger.Send(new UpMessage());
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド上移動要求");
                _logger.LogDebug("UpMessageを送信");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド上移動中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド上移動失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void DownCommand()
        {
            try
            {
                // Messagingを使用してListPanelに下移動要求を送信
                _messenger.Send(new DownMessage());
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド下移動要求");
                _logger.LogDebug("DownMessageを送信");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド下移動中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド下移動失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void ClearCommand()
        {
            try
            {
                // Messagingを使用してListPanelにクリア要求を送信
                _messenger.Send(new ClearMessage());
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] 全コマンドクリア要求");
                _logger.LogDebug("ClearMessageを送信");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドクリア中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンドクリア失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void ClearLog()
        {
            try
            {
                LogEntries.Clear();
                _logger.LogDebug("ログクリア実行");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログクリア中にエラーが発生しました");
            }
        }

        [RelayCommand]
        public async Task RunMacro()
        {
            try
            {
                if (IsRunning)
                {
                    // 実行中の場合は停止
                    await StopMacroInternal();
                    return;
                }

                if (CommandCount == 0)
                {
                    StatusMessage = "実行するコマンドがありません";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: 実行するコマンドがありません");
                    return;
                }

                IsRunning = true;
                StatusMessage = "マクロ実行中...";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行開始 ({CommandCount}個のコマンド)");
                _logger.LogInformation("マクロ実行開始: {Count}個のコマンド", CommandCount);

                // ListPanelに実行開始を通知
                _messenger.Send(new MacroExecutionStateMessage(true));

                // CancellationTokenSourceを作成
                using var cancellationTokenSource = new CancellationTokenSource();
                _currentCancellationTokenSource = cancellationTokenSource;

                try
                {
                    // DIからListPanelViewModelを取得してアイテムを取得
                    var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                    if (listPanelViewModel == null)
                    {
                        throw new InvalidOperationException("ListPanelViewModelがDIから取得できませんでした");
                    }

                    var items = listPanelViewModel.Items;
                    
                    // MacroFactoryでCommandListItemからCommandを生成
                    _logger.LogDebug("MacroFactoryでコマンド生成開始");
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド生成中...");
                    
                    var rootCommand = await Task.Run(() => 
                    {
                        return AutoTool.Model.MacroFactory.MacroFactory.CreateMacro(items);
                    });

                    if (rootCommand == null)
                    {
                        throw new InvalidOperationException("マクロの生成に失敗しました");
                    }

                    _logger.LogDebug("MacroFactoryでコマンド生成完了: {Type}", rootCommand.GetType().Name);
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド生成完了");

                    // 実行前の準備
                    await PrepareForExecution();

                    // マクロを実行
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行開始");
                    _logger.LogInformation("マクロ実行開始");

                    var executionResult = await rootCommand.Execute(cancellationTokenSource.Token);

                    // 実行結果の処理
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        StatusMessage = "マクロ実行が中断されました";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行中断");
                        _logger.LogInformation("マクロ実行中断");
                    }
                    else if (executionResult)
                    {
                        StatusMessage = "マクロ実行完了";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行完了");
                        _logger.LogInformation("マクロ実行完了");
                    }
                    else
                    {
                        StatusMessage = "マクロ実行失敗";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行失敗");
                        _logger.LogWarning("マクロ実行失敗");
                    }
                }
                catch (OperationCanceledException)
                {
                    StatusMessage = "マクロ実行がキャンセルされました";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行キャンセル");
                    _logger.LogInformation("マクロ実行キャンセル");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("ネストが深すぎます"))
                {
                    StatusMessage = "マクロ構造エラー";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ネスト構造が深すぎます");
                    _logger.LogError(ex, "マクロ構造エラー: ネストが深すぎます");
                    MessageBox.Show("マクロのネスト構造が深すぎます。\nLoop や If の入れ子を確認してください。", 
                        "マクロ構造エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("対応する") && ex.Message.Contains("がありません"))
                {
                    StatusMessage = "マクロ構造エラー";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: {ex.Message}");
                    _logger.LogError(ex, "マクロ構造エラー: ペアリング不備");
                    MessageBox.Show($"マクロの構造に問題があります。\n\n{ex.Message}", 
                        "マクロ構造エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    StatusMessage = "マクロ実行エラー";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: マクロ実行失敗 - {ex.Message}");
                    _logger.LogError(ex, "マクロ実行中にエラーが発生しました");
                    MessageBox.Show($"マクロの実行中にエラーが発生しました。\n\n{ex.Message}", 
                        "実行エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _currentCancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacro処理中に予期しないエラーが発生しました");
                StatusMessage = "予期しないエラー";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: 予期しないエラー - {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                
                // ListPanelに実行終了を通知
                _messenger.Send(new MacroExecutionStateMessage(false));
                
                await CleanupAfterExecution();
            }
        }

        [RelayCommand]
        public async Task StopMacro()
        {
            await StopMacroInternal();
        }

        /// <summary>
        /// マクロ停止の内部実装
        /// </summary>
        private async Task StopMacroInternal()
        {
            try
            {
                if (_currentCancellationTokenSource != null)
                {
                    _logger.LogDebug("マクロ実行キャンセル要求");
                    _currentCancellationTokenSource.Cancel();
                    
                    // キャンセルの完了を少し待つ
                    await Task.Delay(100);
                }

                IsRunning = false;
                StatusMessage = "マクロ実行停止";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行停止");
                _logger.LogDebug("マクロ停止完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ停止中にエラーが発生しました");
                IsRunning = false;
                StatusMessage = "停止エラー";
            }
        }

        // ファイル操作コマンド（Messaging使用）
        [RelayCommand]
        public async Task OpenFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "マクロファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                    Title = "マクロファイルを開く"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    StatusMessage = "ファイル読み込み中...";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイル読み込み開始: {openFileDialog.FileName}");

                    try
                    {
                        // Messagingを使用してListPanelにファイル読み込み要求を送信
                        _messenger.Send(new LoadFileMessage(openFileDialog.FileName));
                        
                        StatusMessage = $"ファイルを読み込みました: {Path.GetFileName(openFileDialog.FileName)}";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイル読み込み要求送信");
                        _logger.LogInformation("ファイル読み込み要求: {FileName}", openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = "読み込みエラー";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ファイル読み込み失敗 - {ex.Message}");
                        _logger.LogError(ex, "ファイル読み込み中にエラーが発生しました");
                        MessageBox.Show($"ファイルの読み込み中にエラーが発生しました。\n\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                _logger.LogError(ex, "ファイル読み込み中に予期しないエラーが発生しました");
                StatusMessage = "読み込みエラー";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: 予期しないエラー - {ex.Message}");
                MessageBox.Show($"ファイルの読み込み中に予期しないエラーが発生しました。\n\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task SaveFile()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "マクロファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                    Title = "マクロファイルを保存",
                    DefaultExt = "json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Messagingを使用してListPanelにファイル保存要求を送信
                    _messenger.Send(new SaveFileMessage(saveFileDialog.FileName));
                    
                    StatusMessage = $"ファイルを保存しました: {Path.GetFileName(saveFileDialog.FileName)}";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイル保存要求: {saveFileDialog.FileName}");
                    _logger.LogInformation("ファイル保存要求: {FileName}", saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラーが発生しました");
                StatusMessage = "ファイル保存エラー";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ファイル保存失敗 - {ex.Message}");
                MessageBox.Show($"ファイルの保存に失敗しました。\n\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand] 
        public async Task SaveFileAs() => await SaveFile(); // SaveFileと同じ処理

        [RelayCommand] public void Exit() => Application.Current?.Shutdown();
        [RelayCommand] public void Undo() => _messenger.Send(new UndoMessage());
        [RelayCommand] public void Redo() => _messenger.Send(new RedoMessage());
        [RelayCommand] public void ChangeTheme(string theme) => _logger.LogDebug("テーマ変更: {Theme}（未実装）", theme);
        [RelayCommand] public void RefreshPerformance() => _logger.LogDebug("パフォーマンス更新（未実装）");
        [RelayCommand] public void LoadPluginFile() => _logger.LogDebug("プラグイン読み込み（未実装）");
        [RelayCommand] public void RefreshPlugins() => _logger.LogDebug("プラグイン再読み込み（未実装）");
        [RelayCommand] public void ShowPluginInfo() => _logger.LogDebug("プラグイン情報表示（未実装）");
        [RelayCommand] public void OpenAppDir() => _logger.LogDebug("アプリフォルダ開く（未実装）");
        [RelayCommand] public void ShowAbout() => _logger.LogDebug("バージョン情報表示（未実装）");

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

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(IsListEmpty));
            OnPropertyChanged(nameof(IsListNotEmptyButNoSelection));
            OnPropertyChanged(nameof(IsNotNullItem));
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
        }

        partial void OnSelectedLineNumberChanged(int value)
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
    }
}
