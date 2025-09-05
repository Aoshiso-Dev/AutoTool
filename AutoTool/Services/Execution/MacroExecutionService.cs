using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoTool.Model.CommandDefinition;

using AutoTool.Command.Interface;

namespace AutoTool.Services.Execution
{
    /// <summary>
    /// マクロ実行を管理するサービス
    /// </summary>
    public interface IMacroExecutionService
    {
        bool IsRunning { get; }
        string StatusMessage { get; }
        Task<bool> StartAsync(ObservableCollection<UniversalCommandItem> items);
        Task StopAsync();
        
        event EventHandler<string> StatusChanged;
        event EventHandler<bool> RunningStateChanged;
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

        public bool IsRunning => _isRunning;
        public string StatusMessage => _statusMessage;

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<bool>? RunningStateChanged;

        public MacroExecutionService(ILogger<MacroExecutionService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
                SetRunningState(true);
                SetStatus("実行中...");

                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                // MacroFactoryの設定
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(_serviceProvider);

                var result = await Task.Run(() => ExecuteMacro(items, token), token);
                
                SetStatus(result ? "実行完了" : "一部失敗/中断");
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
                var itemsSnapshot = new List<UniversalCommandItem>(items);
                var root = AutoTool.Model.MacroFactory.MacroFactory.CreateMacro(itemsSnapshot);
                
                var task = root.Execute(cancellationToken);
                
                // 同期的に待機（CancellationTokenを監視）
                while (!task.IsCompleted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(50);
                }
                
                return task.Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                    throw ex.InnerException;
                throw;
            }
        }

        private async Task<bool> ExecuteCommand(UniversalCommandItem item)
        {
            try
            {
                // コマンドの実行ロジック
                await Task.Run(() => { /* コマンド実行処理 */ });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド実行中にエラーが発生しました");
                return false;
            }
        }

        private void SetRunningState(bool isRunning)
        {
            if (_isRunning != isRunning)
            {
                _isRunning = isRunning;
                RunningStateChanged?.Invoke(this, isRunning);
            }
        }

        private void SetStatus(string status)
        {
            if (_statusMessage != status)
            {
                _statusMessage = status;
                StatusChanged?.Invoke(this, status);
            }
        }
    }
}