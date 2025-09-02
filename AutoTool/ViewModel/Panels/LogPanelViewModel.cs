using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5完全統合版：LogPanelViewModel（実際のログ出力機能付き）
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public partial class LogPanelViewModel : ObservableObject
    {
        private readonly ILogger<LogPanelViewModel> _logger;
        private readonly object _lockObject = new object();

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();

        [ObservableProperty]
        private bool _autoScroll = true;

        [ObservableProperty]
        private int _maxLogEntries = 1000;

        /// <summary>
        /// Phase 5完全統合版コンストラクタ
        /// </summary>
        public LogPanelViewModel(ILogger<LogPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // コレクションの変更通知を有効にする
            BindingOperations.EnableCollectionSynchronization(_logEntries, _lockObject);

            WriteLog("Phase 5完全統合版LogPanelViewModel初期化完了");
            _logger.LogInformation("Phase 5完全統合版LogPanelViewModel初期化完了");
        }

        /// <summary>
        /// ログエントリを追加
        /// </summary>
        public void WriteLog(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}";

                lock (_lockObject)
                {
                    // UIスレッドで実行
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(logEntry);

                        // ログエントリ数の制限
                        while (LogEntries.Count > MaxLogEntries)
                        {
                            LogEntries.RemoveAt(0);
                        }
                    });
                }

                _logger.LogDebug("ログエントリ追加: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログエントリ追加中にエラーが発生しました: {Message}", message);
            }
        }

        /// <summary>
        /// ログエントリを追加（行番号とコマンド名付き）
        /// </summary>
        public void WriteLog(string lineNumber, string commandName, string detail)
        {
            var message = $"{lineNumber} {commandName} {detail}";
            WriteLog(message);
        }

        /// <summary>
        /// ログをクリア
        /// </summary>
        public void ClearLog()
        {
            try
            {
                lock (_lockObject)
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Clear();
                    });
                }

                WriteLog("ログをクリアしました");
                _logger.LogDebug("ログをクリアしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログクリア中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 最近のエラーログを取得
        /// </summary>
        public System.Collections.Generic.List<string> GetRecentErrorLines()
        {
            try
            {
                lock (_lockObject)
                {
                    return LogEntries
                        .Where(entry => entry.Contains("?") || entry.Contains("エラー") || entry.Contains("失敗"))
                        .TakeLast(5)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "エラーログ取得中にエラーが発生しました");
                return new System.Collections.Generic.List<string>();
            }
        }

        /// <summary>
        /// ログファイルにエクスポート
        /// </summary>
        public void ExportToFile(string filePath)
        {
            try
            {
                var allLogs = string.Join(Environment.NewLine, LogEntries);
                System.IO.File.WriteAllText(filePath, allLogs);
                
                WriteLog($"ログファイルにエクスポート完了: {filePath}");
                _logger.LogInformation("ログファイルエクスポート完了: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログファイルエクスポート中にエラーが発生しました: {FilePath}", filePath);
                WriteLog($"? ログファイルエクスポートエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ログエントリの統計情報を取得
        /// </summary>
        public (int Total, int Errors, int Warnings) GetLogStatistics()
        {
            try
            {
                lock (_lockObject)
                {
                    var total = LogEntries.Count;
                    var errors = LogEntries.Count(entry => entry.Contains("?") || entry.Contains("エラー"));
                    var warnings = LogEntries.Count(entry => entry.Contains("?") || entry.Contains("警告"));
                    
                    return (total, errors, warnings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログ統計情報取得中にエラーが発生しました");
                return (0, 0, 0);
            }
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        public void Prepare()
        {
            WriteLog("Phase 5完全統合LogPanelViewModel準備完了");
            _logger.LogDebug("Phase 5完全統合LogPanelViewModel準備完了");
        }
    }
}