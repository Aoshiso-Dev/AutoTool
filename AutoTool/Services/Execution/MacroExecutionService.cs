using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;

using AutoTool.Command.Interface;
using AutoTool.ViewModel.Shared;
using AutoTool.Message;

namespace AutoTool.Services.Execution
{
    /// <summary>
    /// マクロ実行を管理するサービス
    /// </summary>
    public interface IMacroExecutionService
    {
        bool IsRunning { get; }
        string StatusMessage { get; }
        int TotalCommands { get; }
        int ExecutedCommands { get; }
        int CurrentLineNumber { get; }
        double OverallProgress { get; }
        
        Task<bool> StartAsync(ObservableCollection<UniversalCommandItem> items);
        Task StopAsync();
        
        event EventHandler<string> StatusChanged;
        event EventHandler<bool> RunningStateChanged;
        event EventHandler<int> CommandCountChanged;
        event EventHandler<int> ExecutedCountChanged;
        event EventHandler<double> ProgressChanged;
    }

    /// <summary>
    /// マクロ実行サービスの実装
    /// </summary>
    public class MacroExecutionService : IMacroExecutionService
    {
        private readonly ILogger<MacroExecutionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning;
        private string _statusMessage = "準備完了";
        private int _totalCommands;
        private int _executedCommands;
        private int _currentLineNumber;
        private double _overallProgress;

        public bool IsRunning => _isRunning;
        public string StatusMessage => _statusMessage;
        public int TotalCommands => _totalCommands;
        public int ExecutedCommands => _executedCommands;
        public int CurrentLineNumber => _currentLineNumber;
        public double OverallProgress => _overallProgress;

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<bool>? RunningStateChanged;
        public event EventHandler<int>? CommandCountChanged;
        public event EventHandler<int>? ExecutedCountChanged;
        public event EventHandler<double>? ProgressChanged;

        public MacroExecutionService(ILogger<MacroExecutionService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // コマンド実行メッセージを購読してプログレス管理
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, OnStartCommandMessage);
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, OnFinishCommandMessage);
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, OnUpdateProgressMessage);
        }

        public async Task<bool> StartAsync(ObservableCollection<UniversalCommandItem> items)
        {
            if (_isRunning)
            {
                _logger.LogWarning("マクロは既に実行中です");
                return false;
            }

            if (items.Count == 0)
            {
                _logger.LogWarning("実行対象のコマンドがありません");
                SetStatus("実行対象がありません");
                return false;
            }

            try
            {
                // 初期化の順序を改善
                _totalCommands = items.Count(item => item.IsEnable);
                _executedCommands = 0;
                _currentLineNumber = 0;
                _overallProgress = 0;
                
                _logger.LogDebug("MacroExecutionService 初期化: TotalCommands={TotalCommands}, ExecutedCommands={ExecutedCommands}, Progress={Progress}",
                    _totalCommands, _executedCommands, _overallProgress);
                
                SetRunningState(true);
                SetStatus("実行中...");
                
                // 初期化の通知（順序を改善）
                NotifyCommandCount(_totalCommands);
                NotifyExecutedCount(_executedCommands);
                NotifyProgress(_overallProgress);

                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                // MacroFactoryの設定
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(_serviceProvider);

                _logger.LogInformation("マクロ実行開始: {ItemCount}個のアイテム (有効: {EnabledCount}個)", 
                    items.Count, _totalCommands);
                
                var result = await Task.Run(() => ExecuteMacro(items, token), token);
                
                SetStatus(result ? "実行完了" : "一部失敗/中断");
                NotifyProgress(result ? 100 : _overallProgress);
                _logger.LogInformation("マクロ実行完了: 結果={Result}", result);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("マクロがキャンセルされました");
                SetStatus("実行キャンセル");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ実行中にエラーが発生しました");
                SetStatus($"実行エラー: {ex.Message}");
                return false;
            }
            finally
            {
                SetRunningState(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning || _cancellationTokenSource == null)
            {
                _logger.LogWarning("停止要求: マクロは実行中ではありません");
                return;
            }

            try
            {
                _logger.LogInformation("マクロ停止要求を受信しました");
                SetStatus("停止要求中...");

                _cancellationTokenSource.Cancel();

                // 強制タイムアウト（5秒）
                await Task.Delay(5000);
                
                if (_isRunning)
                {
                    _logger.LogWarning("マクロが5秒以内に停止しなかったため、強制終了します");
                    SetRunningState(false);
                    SetStatus("強制停止完了");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ停止中にエラーが発生しました");
                SetStatus($"停止エラー: {ex.Message}");
                SetRunningState(false);
            }
        }

        private bool ExecuteMacro(ObservableCollection<UniversalCommandItem> items, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("MacroFactory.CreateMacro呼び出し開始");
                var itemsSnapshot = new List<UniversalCommandItem>(items);
                var root = AutoTool.Model.MacroFactory.MacroFactory.CreateMacro(itemsSnapshot);
                
                // コマンド総数の取得
                _totalCommands = itemsSnapshot.Count;
                CommandCountChanged?.Invoke(this, _totalCommands);
                
                _logger.LogDebug("マクロ実行開始");
                var task = root.Execute(cancellationToken);
                
                // 同期的に待機（CancellationTokenを監視）
                while (!task.IsCompleted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(50);
                }
                
                var result = task.Result;
                _logger.LogDebug("マクロ実行完了: 結果={Result}", result);
                return result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                    throw ex.InnerException;
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteMacro内でエラー発生");
                throw;
            }
        }

        private void SetRunningState(bool isRunning)
        {
            if (_isRunning != isRunning)
            {
                _isRunning = isRunning;
                _logger.LogDebug("実行状態変更: {IsRunning}", isRunning);
                RunningStateChanged?.Invoke(this, isRunning);
            }
        }

        private void SetStatus(string status)
        {
            if (_statusMessage != status)
            {
                _statusMessage = status;
                _logger.LogDebug("ステータス変更: {Status}", status);
                StatusChanged?.Invoke(this, status);
            }
        }

        #region プログレス管理機能

        /// <summary>
        /// コマンド開始メッセージのハンドラー
        /// </summary>
        private void OnStartCommandMessage(object recipient, StartCommandMessage message)
        {
            try
            {
                _currentLineNumber = message.Command.LineNumber;
                _logger.LogDebug("コマンド開始追跡: Line={LineNumber}, Type={Type}", 
                    message.Command.LineNumber, message.Command.GetType().Name);
                
                SetStatus($"実行中: 行 {_currentLineNumber}");
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
                _executedCommands++;
                _currentLineNumber = message.Command.LineNumber;
                
                // 全体のプログレスを計算（上限チェック追加）
                if (_totalCommands > 0)
                {
                    // 実行済み数が総数を超えないように制限
                    var validExecutedCount = Math.Min(_executedCommands, _totalCommands);
                    _overallProgress = Math.Min(100, (validExecutedCount / (double)_totalCommands) * 100);
                }

                _logger.LogDebug("コマンド完了追跡: Line={LineNumber}, 進捗={Progress:F1}% ({Executed}/{Total})", 
                    message.Command.LineNumber, _overallProgress, _executedCommands, _totalCommands);

                // 異常な数値の場合は警告
                if (_executedCommands > _totalCommands)
                {
                    _logger.LogWarning("実行済み数が総数を超過: 実行済み={Executed}, 総数={Total} (重複実行の可能性)", 
                        _executedCommands, _totalCommands);
                }

                NotifyExecutedCount(Math.Min(_executedCommands, _totalCommands));
                NotifyProgress(_overallProgress);
                SetStatus($"実行中: {Math.Min(_executedCommands, _totalCommands)}/{_totalCommands} 完了 ({_overallProgress:F1}%)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FinishCommandMessage処理中にエラー");
            }
        }

        /// <summary>
        /// プログレス更新メッセージのハンドラー
        /// </summary>
        private void OnUpdateProgressMessage(object recipient, UpdateProgressMessage message)
        {
            try
            {
                // 個別コマンドのプログレスを全体プログレスに反映
                if (_totalCommands > 0)
                {
                    // 有効な実行済み数を使用
                    var validExecutedCount = Math.Min(_executedCommands, _totalCommands);
                    var commandProgressContribution = (1.0 / _totalCommands) * (message.Progress / 100.0);
                    var baseProgress = (validExecutedCount / (double)_totalCommands) * 100;
                    _overallProgress = Math.Min(100, baseProgress + (commandProgressContribution * 100));

                    _logger.LogTrace("個別プログレス更新: Line={LineNumber}, CommandProgress={CommandProgress}%, OverallProgress={OverallProgress:F1}%", 
                        message.Command.LineNumber, message.Progress, _overallProgress);

                    NotifyProgress(_overallProgress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProgressMessage処理中にエラー");
            }
        }

        /// <summary>
        /// コマンド数変更を通知
        /// </summary>
        private void NotifyCommandCount(int count)
        {
            CommandCountChanged?.Invoke(this, count);
        }

        /// <summary>
        /// 実行済みコマンド数を通知
        /// </summary>
        private void NotifyExecutedCount(int count)
        {
            ExecutedCountChanged?.Invoke(this, count);
        }

        /// <summary>
        /// プログレス変更を通知
        /// </summary>
        private void NotifyProgress(double progress)
        {
            ProgressChanged?.Invoke(this, progress);
        }

        #endregion
    }
}