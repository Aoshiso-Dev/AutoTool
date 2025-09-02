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
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Panels;
using AutoTool.Message;
using AutoTool.Model.MacroFactory;
using System.Threading;
using System.Diagnostics;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// Phase 5完全統合版：ViewModelファクトリインターフェース
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
    /// Phase 5完全統合版：マクロパネルビューモデル（実際の実行機能付き）
    /// MacroPanels依存を完全削除し、AutoTool統合版のみ使用
    /// </summary>
    public partial class MacroPanelViewModel : ObservableObject
    {
        #region プライベートフィールド

        private readonly ILogger<MacroPanelViewModel> _logger;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly CommandHistoryManager _commandHistory;
        private CancellationTokenSource? _cancellationTokenSource;

        // Phase 5統合版パネルViewModels
        private AutoTool.ViewModel.Panels.ButtonPanelViewModel? _buttonPanelViewModel;
        private AutoTool.ViewModel.Panels.ListPanelViewModel? _listPanelViewModel;
        private AutoTool.ViewModel.Panels.EditPanelViewModel? _editPanelViewModel;
        private AutoTool.ViewModel.Panels.LogPanelViewModel? _logPanelViewModel;
        private AutoTool.ViewModel.Panels.FavoritePanelViewModel? _favoritePanelViewModel;

        #endregion

        #region パブリックプロパティ

        // Phase 5統合版パネルViewModels公開プロパティ
        public AutoTool.ViewModel.Panels.ButtonPanelViewModel? ButtonPanelViewModel => _buttonPanelViewModel;
        public AutoTool.ViewModel.Panels.ListPanelViewModel? ListPanelViewModel => _listPanelViewModel;
        public AutoTool.ViewModel.Panels.EditPanelViewModel? EditPanelViewModel => _editPanelViewModel;
        public AutoTool.ViewModel.Panels.LogPanelViewModel? LogPanelViewModel => _logPanelViewModel;
        public AutoTool.ViewModel.Panels.FavoritePanelViewModel? FavoritePanelViewModel => _favoritePanelViewModel;

        // Phase 5統合版共有サービス
        public CommandHistoryManager CommandHistory => _commandHistory;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "Phase 5完全統合版準備完了";

        [ObservableProperty]
        private int _currentCommandIndex = 0;

        [ObservableProperty]
        private int _totalCommands = 0;

        #endregion

        #region コンストラクタ

        /// <summary>
        /// Phase 5完全統合版コンストラクタ
        /// </summary>
        public MacroPanelViewModel(
            ILogger<MacroPanelViewModel> logger,
            IViewModelFactory viewModelFactory,
            CommandHistoryManager commandHistory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));

            InitializeViewModels();
            SetupMessaging();

            _logger.LogInformation("Phase 5完全統合MacroPanelViewModel初期化完了");
        }

        #endregion

        #region 初期化

        /// <summary>
        /// Phase 5統合版ViewModels初期化
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

                _logger.LogDebug("Phase 5統合版全ViewModels作成完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版ViewModels初期化エラー");
                throw;
            }
        }

        /// <summary>
        /// Phase 5統合版メッセージング設定
        /// </summary>
        private void SetupMessaging()
        {
            try
            {
                // Phase 5: AutoTool統合版メッセージを使用
                WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) =>
                {
                    try
                    {
                        if (_commandHistory.CanUndo)
                        {
                            _commandHistory.Undo();
                            _statusMessage = "元に戻しました";
                            _logger.LogDebug("Phase 5統合版Undo実行完了");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Phase 5統合版Undo実行エラー");
                        _statusMessage = $"元に戻しエラー: {ex.Message}";
                    }
                });

                WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) =>
                {
                    try
                    {
                        if (_commandHistory.CanRedo)
                        {
                            _commandHistory.Redo();
                            _statusMessage = "やり直しました";
                            _logger.LogDebug("Phase 5統合版Redo実行完了");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Phase 5統合版Redo実行エラー");
                        _statusMessage = $"やり直しエラー: {ex.Message}";
                    }
                });

                // 実行制御メッセージ
                WeakReferenceMessenger.Default.Register<RunMessage>(this, async (r, m) =>
                {
                    await RunMacroAsync();
                });

                WeakReferenceMessenger.Default.Register<StopMessage>(this, (r, m) =>
                {
                    StopMacro();
                });

                // コマンド実行関連メッセージ
                WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) =>
                {
                    LogCommandStart(m);
                });

                WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) =>
                {
                    LogCommandFinish(m);
                });

                WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, (r, m) =>
                {
                    LogCommandProgress(m);
                });

                // 選択変更メッセージ
                WeakReferenceMessenger.Default.Register<ChangeSelectedMessage>(this, (r, m) =>
                {
                    _editPanelViewModel?.SetItem(m.Item);
                });

                _logger.LogDebug("Phase 5統合版メッセージング設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版メッセージング設定エラー");
            }
        }

        #endregion

        #region Phase 5統合版マクロ実行機能

        /// <summary>
        /// Phase 5統合版：マクロ実行
        /// </summary>
        [RelayCommand]
        public async Task RunMacroAsync()
        {
            try
            {
                if (IsRunning) return;

                IsRunning = true;
                _statusMessage = "Phase 5統合版マクロ実行中...";
                _logger.LogInformation("Phase 5統合版マクロ実行開始");

                // 全ViewModelの実行状態を設定
                SetAllViewModelsRunningState(true);

                // コマンドリストを取得
                var commandItems = _listPanelViewModel?.Items?.ToList();
                if (commandItems == null || !commandItems.Any())
                {
                    _statusMessage = "実行するコマンドがありません";
                    _logger.LogWarning("実行するコマンドがありません");
                    return;
                }

                // キャンセレーショントークンを設定
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                TotalCommands = commandItems.Count;
                CurrentCommandIndex = 0;

                _logPanelViewModel?.WriteLog($"=== マクロ実行開始 ({TotalCommands}件) ===");

                // MacroFactoryを使用してコマンドを作成
                var macro = MacroFactory.CreateMacro(commandItems);

                // マクロを実行
                var stopwatch = Stopwatch.StartNew();
                var result = await Task.Run(() => macro.Execute(cancellationToken), cancellationToken);
                stopwatch.Stop();

                if (result)
                {
                    _statusMessage = $"Phase 5統合版マクロ実行完了 ({stopwatch.ElapsedMilliseconds}ms)";
                    _logPanelViewModel?.WriteLog($"=== マクロ実行完了 ({stopwatch.ElapsedMilliseconds}ms) ===");
                    _logger.LogInformation("Phase 5統合版マクロ実行完了");
                }
                else
                {
                    _statusMessage = "Phase 5統合版マクロ実行失敗";
                    _logPanelViewModel?.WriteLog("=== マクロ実行失敗 ===");
                    _logger.LogWarning("Phase 5統合版マクロ実行失敗");
                }
            }
            catch (OperationCanceledException)
            {
                _statusMessage = "Phase 5統合版マクロ実行キャンセル";
                _logPanelViewModel?.WriteLog("=== マクロ実行キャンセル ===");
                _logger.LogInformation("Phase 5統合版マクロ実行キャンセル");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版マクロ実行エラー");
                _statusMessage = $"実行エラー: {ex.Message}";
                _logPanelViewModel?.WriteLog($"❌ マクロ実行エラー: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                SetAllViewModelsRunningState(false);
                CurrentCommandIndex = 0;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Phase 5統合版：マクロ停止
        /// </summary>
        [RelayCommand]
        public void StopMacro()
        {
            try
            {
                if (!IsRunning) return;

                _cancellationTokenSource?.Cancel();
                _statusMessage = "Phase 5統合版マクロ停止中...";
                _logger.LogInformation("Phase 5統合版マクロ停止要求");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版マクロ停止エラー");
                _statusMessage = $"停止エラー: {ex.Message}";
            }
        }

        private void SetAllViewModelsRunningState(bool isRunning)
        {
            _buttonPanelViewModel?.SetRunningState(isRunning);
            _listPanelViewModel?.SetRunningState(isRunning);
            _editPanelViewModel?.SetRunningState(isRunning);
            _logPanelViewModel?.SetRunningState(isRunning);
            _favoritePanelViewModel?.SetRunningState(isRunning);

            _logger.LogDebug("全ViewModelの実行状態を設定: {IsRunning}", isRunning);
        }

        #endregion

        #region ログ・監視機能

        private void LogCommandStart(StartCommandMessage message)
        {
            try
            {
                var command = message.Command;
                CurrentCommandIndex = command.LineNumber;
                
                _logPanelViewModel?.WriteLog($"[{command.LineNumber:D2}] {command.Description} 開始");
                _logger.LogDebug("コマンド開始: Line={Line}, Description={Description}", command.LineNumber, command.Description);
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
                _logPanelViewModel?.WriteLog($"[{command.LineNumber:D2}] {command.Description} 完了");
                _logger.LogDebug("コマンド完了: Line={Line}, Description={Description}", command.LineNumber, command.Description);
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
                _logPanelViewModel?.WriteLog($"[{command.LineNumber:D2}] {message.Detail}");
                _logger.LogDebug("コマンド進行: Line={Line}, Detail={Detail}", command.LineNumber, message.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド進行ログ出力エラー");
            }
        }

        #endregion

        #region コマンド編集

        /// <summary>
        /// Phase 5統合版：コマンド編集
        /// </summary>
        public void EditCommand(ICommandListItem item)
        {
            try
            {
                if (item == null || _editPanelViewModel == null) return;

                // Phase 5: 編集プロパティの設定（簡易実装）
                _statusMessage = $"編集中: {item.ItemType}";
                _logger.LogDebug("Phase 5統合版コマンド編集開始: {ItemType}", item.ItemType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版コマンド編集エラー");
                _statusMessage = $"編集エラー: {ex.Message}";
            }
        }

        #endregion

        #region ファイル操作

        /// <summary>
        /// Phase 5統合版：マクロファイル読み込み
        /// </summary>
        public void LoadMacroFile(string filePath)
        {
            try
            {
                _isLoading = true;
                _statusMessage = $"ファイル読み込み中: {Path.GetFileName(filePath)}";

                _listPanelViewModel?.Load(filePath);
                _commandHistory.Clear(); // ファイル読み込み後は履歴をクリア

                _statusMessage = $"読み込み完了: {Path.GetFileName(filePath)}";
                _logger.LogInformation("Phase 5統合版マクロファイル読み込み: {File}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版マクロファイル読み込みエラー: {File}", filePath);
                _statusMessage = $"読み込みエラー: {ex.Message}";
                throw;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Phase 5統合版：マクロファイル保存
        /// </summary>
        public void SaveMacroFile(string filePath)
        {
            try
            {
                _isLoading = true;
                _statusMessage = $"ファイル保存中: {Path.GetFileName(filePath)}";

                _listPanelViewModel?.Save(filePath);

                _statusMessage = $"保存完了: {Path.GetFileName(filePath)}";
                _logger.LogInformation("Phase 5統合版マクロファイル保存: {File}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版マクロファイル保存エラー: {File}", filePath);
                _statusMessage = $"保存エラー: {ex.Message}";
                throw;
            }
            finally
            {
                _isLoading = false;
            }
        }

        #endregion

        #region クリーンアップ

        /// <summary>
        /// Phase 5統合版：リソースクリーンアップ
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                WeakReferenceMessenger.Default.UnregisterAll(this);
                _logger.LogDebug("Phase 5統合版MacroPanelViewModel クリーンアップ完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版MacroPanelViewModel クリーンアップエラー");
            }
        }

        #endregion
    }
}