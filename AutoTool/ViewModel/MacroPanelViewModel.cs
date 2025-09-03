using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Panels;
using AutoTool.Message;
using AutoTool.Model.MacroFactory;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.Services.Plugin;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// ViewModelファクトリインターフェース
    /// </summary>
    public interface IViewModelFactory
    {
        AutoTool.ViewModel.Panels.ButtonPanelViewModel CreateButtonPanelViewModel();
        AutoTool.ViewModel.Panels.ListPanelViewModel CreateListPanelViewModel();
        AutoTool.ViewModel.Panels.EditPanelViewModel CreateEditPanelViewModel();
        AutoTool.ViewModel.Panels.LogPanelViewModel CreateLogPanelViewModel();
        AutoTool.ViewModel.Panels.FavoritePanelViewModel CreateFavoritePanelViewModel();
    }

    /// <summary>
    /// コマンド実行統計
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
    /// マクロパネルビューモデル（実際の実行機能付き）
    /// MacroPanels依存を完全削除し、AutoTool統合版のみ使用
    /// </summary>
    public partial class MacroPanelViewModel : ObservableObject
    {
        #region プライベートフィールド

        private readonly ILogger<MacroPanelViewModel> _logger;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly CommandHistoryManager _commandHistory;
        private readonly System.IServiceProvider? _serviceProvider;
        private CancellationTokenSource? _cancellationTokenSource;
        private ICommand? _currentMacroCommand;

        // パネルViewModels
        private AutoTool.ViewModel.Panels.ButtonPanelViewModel? _buttonPanelViewModel;
        private AutoTool.ViewModel.Panels.ListPanelViewModel? _listPanelViewModel;
        private AutoTool.ViewModel.Panels.EditPanelViewModel? _editPanelViewModel;
        private AutoTool.ViewModel.Panels.LogPanelViewModel? _logPanelViewModel;
        private AutoTool.ViewModel.Panels.FavoritePanelViewModel? _favoritePanelViewModel;

        #endregion

        #region パブリックプロパティ

        // パネルViewModels公開プロパティ
        public AutoTool.ViewModel.Panels.ButtonPanelViewModel? ButtonPanelViewModel => _buttonPanelViewModel;
        public AutoTool.ViewModel.Panels.ListPanelViewModel? ListPanelViewModel => _listPanelViewModel;
        public AutoTool.ViewModel.Panels.EditPanelViewModel? EditPanelViewModel => _editPanelViewModel;
        public AutoTool.ViewModel.Panels.LogPanelViewModel? LogPanelViewModel => _logPanelViewModel;
        public AutoTool.ViewModel.Panels.FavoritePanelViewModel? FavoritePanelViewModel => _favoritePanelViewModel;

        // 共有サービス
        public CommandHistoryManager CommandHistory => _commandHistory;

        private bool _isRunning = false;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = "準備完了";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private int _currentCommandIndex = 0;
        public int CurrentCommandIndex
        {
            get => _currentCommandIndex;
            set => SetProperty(ref _currentCommandIndex, value);
        }

        private int _totalCommands = 0;
        public int TotalCommands
        {
            get => _totalCommands;
            set => SetProperty(ref _totalCommands, value);
        }

        private CommandExecutionStats _executionStats = new();
        public CommandExecutionStats ExecutionStats
        {
            get => _executionStats;
            set => SetProperty(ref _executionStats, value);
        }

        private double _overallProgress = 0.0;
        public double OverallProgress
        {
            get => _overallProgress;
            set => SetProperty(ref _overallProgress, value);
        }

        private string _currentCommandDescription = "";
        public string CurrentCommandDescription
        {
            get => _currentCommandDescription;
            set => SetProperty(ref _currentCommandDescription, value);
        }

        #endregion

        #region コンストラクタ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MacroPanelViewModel(
            ILogger<MacroPanelViewModel> logger,
            IViewModelFactory viewModelFactory,
            CommandHistoryManager commandHistory,
            System.IServiceProvider? serviceProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
            _serviceProvider = serviceProvider;

            InitializeViewModels();
            SetupMessaging();

            _logger.LogInformation("MacroPanelViewModel初期化完了");
        }

        /// <summary>
        /// 各ViewModelを直接受け取るコンストラクタ（DI対応）
        /// </summary>
        public MacroPanelViewModel(
            ILogger<MacroPanelViewModel> logger,
            AutoTool.ViewModel.Panels.ButtonPanelViewModel buttonPanelViewModel,
            AutoTool.ViewModel.Panels.ListPanelViewModel listPanelViewModel,
            AutoTool.ViewModel.Panels.EditPanelViewModel editPanelViewModel,
            AutoTool.ViewModel.Panels.LogPanelViewModel logPanelViewModel,
            AutoTool.ViewModel.Panels.FavoritePanelViewModel favoritePanelViewModel,
            CommandHistoryManager commandHistory,
            System.IServiceProvider? serviceProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
            _serviceProvider = serviceProvider;
            
            // 各ViewModelを直接設定
            _buttonPanelViewModel = buttonPanelViewModel ?? throw new ArgumentNullException(nameof(buttonPanelViewModel));
            _listPanelViewModel = listPanelViewModel ?? throw new ArgumentNullException(nameof(listPanelViewModel));
            _editPanelViewModel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
            _logPanelViewModel = logPanelViewModel ?? throw new ArgumentNullException(nameof(logPanelViewModel));
            _favoritePanelViewModel = favoritePanelViewModel ?? throw new ArgumentNullException(nameof(favoritePanelViewModel));

            SetupMessaging();

            _logger.LogInformation("MacroPanelViewModel初期化完了（DI直接注入）");
        }

        #endregion

        #region 初期化

        /// <summary>
        /// ViewModels初期化
        /// </summary>
        private void InitializeViewModels()
        {
            try
            {
                _buttonPanelViewModel = _viewModelFactory.CreateButtonPanelViewModel();
                _listPanelViewModel = _viewModelFactory.CreateListPanelViewModel();
                _editPanelViewModel = _viewModelFactory.CreateEditPanelViewModel();
                _logPanelViewModel = _viewModelFactory.CreateLogPanelViewModel();
                _favoritePanelViewModel = _viewModelFactory.CreateFavoritePanelViewModel();

                _logger.LogDebug("全ViewModels作成完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModels初期化エラー");
                throw;
            }
        }

        /// <summary>
        /// メッセージング設定
        /// </summary>
        private void SetupMessaging()
        {
            try
            {
                // Undo/Redo
                WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) =>
                {
                    try
                    {
                        if (_commandHistory.CanUndo)
                        {
                            _commandHistory.Undo();
                            StatusMessage = "元に戻しました";
                            _logger.LogDebug("Undo実行完了");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Undo実行エラー");
                        StatusMessage = $"元に戻しエラー: {ex.Message}";
                    }
                });

                WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) =>
                {
                    try
                    {
                        if (_commandHistory.CanRedo)
                        {
                            _commandHistory.Redo();
                            StatusMessage = "やり直しました";
                            _logger.LogDebug("Redo実行完了");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Redo実行エラー");
                        StatusMessage = $"やり直しエラー: {ex.Message}";
                    }
                });

                // 実行制御メッセージ
                WeakReferenceMessenger.Default.Register<RunMessage>(this, async (r, m) =>
                {
                    await RunMacroCommand();
                });

                WeakReferenceMessenger.Default.Register<StopMessage>(this, (r, m) =>
                {
                    StopMacroCommand();
                });

                // コマンド実行関連メッセージ
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

                // リスト操作メッセージ
                WeakReferenceMessenger.Default.Register<AddMessage>(this, (r, m) =>
                {
                    _logger.LogInformation("コマンド追加要求: {ItemType}", m.ItemType);
                    StatusMessage = $"コマンドを追加中: {m.ItemType}";
                });

                WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) =>
                {
                    _logger.LogInformation("コマンド削除要求");
                    StatusMessage = "コマンドを削除中...";
                });

                WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) =>
                {
                    _logger.LogInformation("全コマンドクリア要求");
                    StatusMessage = "全コマンドをクリア中...";
                });

                // ファイル操作メッセージ
                WeakReferenceMessenger.Default.Register<SaveMessage>(this, (r, m) =>
                {
                    _logger.LogInformation("保存要求");
                });

                WeakReferenceMessenger.Default.Register<LoadMessage>(this, (r, m) =>
                {
                    _logger.LogInformation("読み込み要求");
                });

                // 選択変更メッセージ
                WeakReferenceMessenger.Default.Register<ChangeSelectedMessage>(this, (r, m) =>
                {
                    _editPanelViewModel?.SetItem(m.Item);
                });

                // アイテムタイプ変更メッセージ
                WeakReferenceMessenger.Default.Register<ChangeItemTypeMessage>(this, (r, m) =>
                {
                    _logger.LogDebug("アイテムタイプ変更要求: {OldType} -> {NewType}", m.OldItem.ItemType, m.NewItem.ItemType);
                    StatusMessage = $"タイプ変更: {m.OldItem.ItemType} -> {m.NewItem.ItemType}";
                });

                // リストビュー更新メッセージ
                WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (r, m) =>
                {
                    _logger.LogDebug("リストビュー更新要求");
                    StatusMessage = "リストビューを更新中...";
                });

                _logger.LogDebug("メッセージング設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "メッセージング設定エラー");
            }
        }

        #endregion

        #region マクロ実行機能

        /// <summary>
        /// マクロ実行（MacroFactoryを使用した実際の実行）
        /// </summary>
        [RelayCommand]
        public async Task RunMacroCommand()
        {
            if (IsRunning)
            {
                _logger.LogWarning("マクロが既に実行中です");
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
                
                // 実行統計の初期化
                ExecutionStats = new CommandExecutionStats
                {
                    StartTime = DateTime.Now
                };
                
                // すべてのアイテムの実行状態をリセット
                ResetAllCommandStates();
                
                _cancellationTokenSource = new CancellationTokenSource();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                _logPanelViewModel?.WriteLog("=== マクロ実行開始 ===");
                _logger.LogInformation("マクロ実行開始");

                // コマンドリストを取得して検証
                var commandItems = _listPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                var validation = ValidateCommandList(commandItems);
                
                if (!validation.IsValid)
                {
                    StatusMessage = $"検証エラー: {validation.ErrorMessage}";
                    _logPanelViewModel?.WriteLog($"❌ 検証エラー: {validation.ErrorMessage}");
                    
                    if (!string.IsNullOrEmpty(validation.WarningMessage))
                    {
                        _logPanelViewModel?.WriteLog($"⚠️ 警告: {validation.WarningMessage}");
                    }
                    
                    return;
                }

                // 警告があってもログ出力
                if (!string.IsNullOrEmpty(validation.WarningMessage))
                {
                    _logPanelViewModel?.WriteLog($"⚠️ 警告: {validation.WarningMessage}");
                }

                TotalCommands = commandItems.Count(x => x.IsEnable);
                ExecutionStats.TotalCommands = TotalCommands;
                
                StatusMessage = $"マクロ実行中... ({TotalCommands}コマンド)";

                bool result = false;

                // ServiceProviderの設定とMacroFactoryを使った実際のコマンド実行
                if (_serviceProvider != null && commandItems.Count > 0)
                {
                    try
                    {
                        // MacroFactoryにServiceProviderを設定
                        MacroFactory.SetServiceProvider(_serviceProvider);
                        
                        // プラグインサービスも設定（もし存在する場合）
                        var pluginService = _serviceProvider.GetService<IPluginService>();
                        if (pluginService != null)
                        {
                            MacroFactory.SetPluginService(pluginService);
                        }

                        _logPanelViewModel?.WriteLog($"🔧 MacroFactoryでマクロコマンドを作成中... ({commandItems.Count}アイテム)");
                        
                        // MacroFactoryを使ってコマンド階層を作成
                        _currentMacroCommand = MacroFactory.CreateMacro(commandItems);
                        
                        _logPanelViewModel?.WriteLog("✅ マクロコマンド作成完了");
                        _logger.LogInformation("MacroFactoryでマクロコマンドを作成しました");

                        // 実行コンテキストを作成
                        var variableStore = _serviceProvider.GetService<IVariableStore>();
                        var executionContext = new CommandExecutionContext(
                            _cancellationTokenSource.Token, 
                            variableStore, 
                            _serviceProvider);

                        // 実行コンテキストを設定
                        if (_currentMacroCommand is BaseCommand baseCommand)
                        {
                            baseCommand.SetExecutionContext(executionContext);
                        }

                        // 実際のマクロコマンドを実行
                        _logPanelViewModel?.WriteLog("🚀 マクロ実行開始");
                        result = await _currentMacroCommand.Execute(_cancellationTokenSource.Token);
                        
                        stopwatch.Stop();
                        _logPanelViewModel?.WriteLog($"=== マクロ実行完了 ({stopwatch.ElapsedMilliseconds}ms) ===");
                    }
                    catch (OperationCanceledException)
                    {
                        _logPanelViewModel?.WriteLog("=== マクロ実行キャンセル ===");
                        result = false;
                        throw;
                    }
                    catch (FileNotFoundException ex)
                    {
                        _logger.LogError(ex, "ファイルが見つかりません");
                        _logPanelViewModel?.WriteLog($"❌ ファイルエラー: {ex.Message}");
                        result = false;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        _logger.LogError(ex, "ディレクトリが見つかりません");
                        _logPanelViewModel?.WriteLog($"❌ ディレクトリエラー: {ex.Message}");
                        result = false;
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError(ex, "マクロ構造エラー");
                        _logPanelViewModel?.WriteLog($"❌ マクロ構造エラー: {ex.Message}");
                        result = false;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "マクロ実行エンジンエラー");
                        _logPanelViewModel?.WriteLog($"❌ マクロ実行エンジンエラー: {ex.Message}");
                        result = false;
                    }
                }
                else
                {
                    // ServiceProviderがない場合の簡易ダミー実行
                    _logPanelViewModel?.WriteLog("⚠️ ServiceProvider未設定のためダミー実行モード");
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

                // 実行結果に応じたログ出力
                if (result)
                {
                    var successRate = ExecutionStats.SuccessRate;
                    StatusMessage = $"マクロ実行完了 ({stopwatch.ElapsedMilliseconds}ms, 成功率: {successRate:F1}%)";
                    _logPanelViewModel?.WriteLog($"✅ 全て成功! 実行時間: {stopwatch.ElapsedMilliseconds}ms");
                    _logger.LogInformation("マクロ実行完了: 成功率={SuccessRate:F1}%, 時間={Duration}ms", successRate, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    StatusMessage = $"マクロ実行失敗 ({ExecutionStats.FailedCommands}/{ExecutionStats.TotalCommands}個失敗)";
                    _logPanelViewModel?.WriteLog($"❌ 実行失敗: {ExecutionStats.FailedCommands}個のコマンドが失敗");
                    _logger.LogWarning("マクロ実行失敗: 失敗コマンド={Failed}/{Total}", ExecutionStats.FailedCommands, ExecutionStats.TotalCommands);
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
                _logPanelViewModel?.WriteLog("=== マクロ実行キャンセル ===");
                _logger.LogInformation("マクロ実行キャンセル");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ実行中に予期しないエラーが発生しました");
                StatusMessage = $"実行エラー: {ex.Message}";
                _logPanelViewModel?.WriteLog($"❌ 予期しないエラー: {ex.Message}");
                
                // エラーメッセージ送信
                WeakReferenceMessenger.Default.Send(new StatusUpdateMessage("Error", ex.Message));
            }
            finally
            {
                // クリーンアップ処理
                IsRunning = false;
                SetAllViewModelsRunningState(false);
                CurrentCommandIndex = 0;
                CurrentCommandDescription = "";
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _currentMacroCommand = null;
                
                // 実行完了後、すべてのアイテムの実行状態をクリア
                ResetAllCommandStates();
            }
        }

        /// <summary>
        /// マクロ停止
        /// </summary>
        [RelayCommand]
        public void StopMacroCommand()
        {
            try
            {
                if (!IsRunning) return;

                _cancellationTokenSource?.Cancel();
                StatusMessage = "マクロ停止中...";
                _logger.LogInformation("マクロ停止要求");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ停止エラー");
                StatusMessage = $"停止エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// ServiceProvider未設定時のダミー実行モード
        /// </summary>
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
                        // ダミーコマンド開始メッセージ
                        var dummyCommand = new DummyCommand 
                        { 
                            LineNumber = item.LineNumber, 
                            Description = item.ItemType 
                        };
                        
                        WeakReferenceMessenger.Default.Send(new StartCommandMessage(dummyCommand));
                        
                        // プログレス更新のシミュレーション
                        for (int progress = 0; progress <= 100; progress += 20)
                        {
                            if (_cancellationTokenSource?.Token.IsCancellationRequested == true) 
                                break;
                                
                            item.Progress = progress;
                            WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(dummyCommand, progress));
                            await Task.Delay(100, _cancellationTokenSource?.Token ?? CancellationToken.None);
                        }
                        
                        // 全体進捗更新
                        OverallProgress = ((double)(i + 1) / enabledItems.Count) * 100.0;
                        
                        // コマンド完了メッセージ送信
                        WeakReferenceMessenger.Default.Send(new FinishCommandMessage(dummyCommand));
                        
                        _logPanelViewModel?.WriteLog($"✅ 実行完了: {item.ItemType} (行 {item.LineNumber})");
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
                _logger.LogError(ex, "ダミー実行モードでエラーが発生しました");
                return false;
            }
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// すべてのコマンドの実行状態をリセット
        /// </summary>
        private void ResetAllCommandStates()
        {
            try
            {
                var items = _listPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                foreach (var item in items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }
                _logger.LogDebug("すべてのコマンドの実行状態をリセットしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド状態リセット中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 全ViewModelの実行状態を設定
        /// </summary>
        private void SetAllViewModelsRunningState(bool isRunning)
        {
            try
            {
                _buttonPanelViewModel?.SetRunningState(isRunning);
                _listPanelViewModel?.SetRunningState(isRunning);
                _editPanelViewModel?.SetRunningState(isRunning);
                _logPanelViewModel?.SetRunningState(isRunning);
                _favoritePanelViewModel?.SetRunningState(isRunning);

                _logger.LogDebug("全ViewModelの実行状態を設定: {IsRunning}", isRunning);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModel実行状態設定中にエラーが発生しました");
            }
        }

        /// <summary>
        /// サービスを取得
        /// </summary>
        private T? GetService<T>() where T : class
        {
            if (_serviceProvider == null) return null;
            return _serviceProvider.GetService(typeof(T)) as T;
        }

        #endregion

        #region メッセージハンドリング

        /// <summary>
        /// コマンドエラーログ処理
        /// </summary>
        private void LogCommandError(CommandErrorMessage message)
        {
            try
            {
                var command = message.Command;
                var ex = message.Exception;
                
                // エラータイプに応じた詳細メッセージ
                var errorDetail = ex switch
                {
                    FileNotFoundException => $"ファイルが見つかりません: {ex.Message}",
                    DirectoryNotFoundException => $"ディレクトリが見つかりません: {ex.Message}",
                    TimeoutException => $"タイムアウトしました: {ex.Message}",
                    OperationCanceledException => "操作がキャンセルされました",
                    InvalidOperationException => $"操作が無効です: {ex.Message}",
                    _ => $"予期しないエラー: {ex.Message}"
                };

                _logPanelViewModel?.WriteLog($"❌ [{command.LineNumber:D2}] {command.Description}: {errorDetail}");
                _logger.LogError(ex, "コマンドエラー: Line={Line}, Description={Description}", 
                    command.LineNumber, command.Description);

                ExecutionStats.FailedCommands++;
                
                // 対応するアイテムの実行状態を更新
                UpdateItemErrorState(command.LineNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドエラーログ出力中にエラーが発生しました");
            }
        }

        /// <summary>
        /// アイテムの進捗を更新
        /// </summary>
        private void UpdateItemProgress(UpdateProgressMessage message)
        {
            try
            {
                var items = _listPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                var targetItem = items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                
                if (targetItem != null)
                {
                    targetItem.Progress = message.Progress;
                }

                // 全体進捗の計算
                var enabledItems = items.Where(x => x.IsEnable).ToList();
                if (enabledItems.Count > 0)
                {
                    var totalProgress = enabledItems.Sum(x => x.Progress);
                    OverallProgress = totalProgress / enabledItems.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "進捗更新中にエラーが発生しました");
            }
        }

        /// <summary>
        /// コマンド進捗更新
        /// </summary>
        private void UpdateCommandProgress(ICommand command, bool isFinished = false)
        {
            try
            {
                CurrentCommandDescription = $"{command.Description} (行 {command.LineNumber})";
                
                var items = _listPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
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
                _logger.LogError(ex, "コマンド進捗更新中にエラーが発生しました");
            }
        }

        /// <summary>
        /// アイテムのエラー状態を更新
        /// </summary>
        private void UpdateItemErrorState(int lineNumber)
        {
            try
            {
                var items = _listPanelViewModel?.Items?.ToList() ?? new List<ICommandListItem>();
                var targetItem = items.FirstOrDefault(x => x.LineNumber == lineNumber);
                
                if (targetItem != null)
                {
                    targetItem.IsRunning = false;
                    targetItem.Progress = 0; // エラー時は進捗をリセット
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムエラー状態更新中にエラーが発生しました");
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
                _logPanelViewModel?.WriteLog($"[{timestamp}][{command.LineNumber:D2}] ▶ {command.Description} 開始");
                _logger.LogDebug("コマンド開始: Line={Line}, Description={Description}", 
                    command.LineNumber, command.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド開始ログ出力エラー");
            }
        }

        private void LogCommandFinish(FinishCommandMessage message)
        {
            try
            {
                var command = message.Command;
                var timestamp = message.Timestamp.ToString("HH:mm:ss.fff");
                _logPanelViewModel?.WriteLog($"[{timestamp}][{command.LineNumber:D2}] ✓ {command.Description} 完了");
                _logger.LogDebug("コマンド完了: Line={Line}, Description={Description}", 
                    command.LineNumber, command.Description);
                    
                ExecutionStats.SuccessfulCommands++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド完了ログ出力エラー");
            }
        }

        private void LogCommandProgress(DoingCommandMessage message)
        {
            try
            {
                var command = message.Command;
                var timestamp = message.Timestamp.ToString("HH:mm:ss.fff");
                _logPanelViewModel?.WriteLog($"[{timestamp}][{command.LineNumber:D2}] → {message.Detail}");
                _logger.LogDebug("コマンド進行: Line={Line}, Detail={Detail}", 
                    command.LineNumber, message.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド進行ログ出力エラー");
            }
        }

        #endregion

        #region コマンドリスト検証

        /// <summary>
        /// コマンドリストの検証（改良版）
        /// </summary>
        private ValidationResult ValidateCommandList(List<ICommandListItem> commandItems)
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // 基本検証
                if (commandItems.Count == 0)
                {
                    return new ValidationResult 
                    { 
                        IsValid = false, 
                        ErrorMessage = "実行するコマンドがありません" 
                    };
                }

                var enabledItems = commandItems.Where(x => x.IsEnable).ToList();
                if (enabledItems.Count == 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "有効なコマンドがありません（すべて無効化されています）"
                    };
                }

                // ペア検証の改良
                ValidatePairStructure(enabledItems, errors, warnings);
                
                // ファイル存在チェックの改良
                ValidateRequiredFiles(enabledItems, errors, warnings);
                
                // 設定値検証
                ValidateCommandSettings(enabledItems, errors, warnings);

                var result = new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    ErrorMessage = errors.Count > 0 ? string.Join("\n", errors) : "",
                    WarningMessage = warnings.Count > 0 ? string.Join("\n", warnings) : "",
                    ErrorCount = errors.Count,
                    WarningCount = warnings.Count
                };

                if (result.WarningCount > 0)
                {
                    _logger.LogWarning("検証警告: {WarningCount}件", result.WarningCount);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドリスト検証中に予期しないエラーが発生しました");
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = $"検証処理エラー: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// ペア構造の検証
        /// </summary>
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

            // 未閉じペアのチェック
            if (loopStack.Count > 0)
            {
                errors.Add($"閉じられていないLoopがあります: {loopStack.Count}個");
            }

            if (ifStack.Count > 0)
            {
                errors.Add($"閉じられていないIfがあります: {ifStack.Count}個");
            }
        }

        /// <summary>
        /// 必須ファイルの検証
        /// </summary>
        private void ValidateRequiredFiles(List<ICommandListItem> items, List<string> errors, List<string> warnings)
        {
            foreach (var item in items)
            {
                try
                {
                    // ファイル存在チェック（改良版）
                    ValidateItemFiles(item, errors, warnings);
                }
                catch (Exception ex)
                {
                    warnings.Add($"行 {item.LineNumber}: ファイル検証中にエラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// アイテムのファイル検証
        /// </summary>
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

            // ディレクトリ検証
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
                        // 保存先は親ディレクトリが存在すればOK
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

        /// <summary>
        /// コマンド設定値の検証
        /// </summary>
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

        #region コマンド編集

        /// <summary>
        /// コマンド編集
        /// </summary>
        public void EditCommand(ICommandListItem item)
        {
            try
            {
                if (item == null || _editPanelViewModel == null) return;

                // 編集プロパティの設定（簡易実装）
                StatusMessage = $"編集中: {item.ItemType}";
                _logger.LogDebug("コマンド編集開始: {ItemType}", item.ItemType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド編集エラー");
                StatusMessage = $"編集エラー: {ex.Message}";
            }
        }

        #endregion

        #region ファイル操作

        /// <summary>
        /// マクロファイル読み込み
        /// </summary>
        public void LoadMacroFile(string filePath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = $"ファイル読み込み中: {Path.GetFileName(filePath)}";

                _listPanelViewModel?.Load(filePath);
                _commandHistory.Clear(); // ファイル読み込み後は履歴をクリア

                StatusMessage = $"読み込み完了: {Path.GetFileName(filePath)}";
                _logger.LogInformation("マクロファイル読み込み: {File}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロファイル読み込みエラー: {File}", filePath);
                StatusMessage = $"読み込みエラー: {ex.Message}";
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// マクロファイル保存
        /// </summary>
        public void SaveMacroFile(string filePath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = $"ファイル保存中: {Path.GetFileName(filePath)}";

                _listPanelViewModel?.Save(filePath);

                StatusMessage = $"保存完了: {Path.GetFileName(filePath)}";
                _logger.LogInformation("マクロファイル保存: {File}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロファイル保存エラー: {File}", filePath);
                StatusMessage = $"保存エラー: {ex.Message}";
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region リソース管理

        /// <summary>
        /// リソースクリーンアップ（改良版）
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // マクロ実行の強制停止
                if (IsRunning)
                {
                    StopMacroCommand();
                }

                // リソースの解放
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _currentMacroCommand = null;

                // メッセージング解除
                WeakReferenceMessenger.Default.UnregisterAll(this);
                
                _logger.LogDebug("MacroPanelViewModel クリーンアップ完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MacroPanelViewModel クリーンアップエラー");
            }
        }

        #endregion
    }

    /// <summary>
    /// 検証結果クラス
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
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

    /// <summary>
    /// 実行統計クラス（内部使用）
    /// </summary>
    internal class InternalExecutionStats
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public int TotalCommands { get; set; }
        public int ExecutedCommands { get; set; }
        public int SuccessfulCommands { get; set; }
        public int FailedCommands { get; set; }
    }
}