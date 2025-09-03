using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5完全統合版：LogPanelViewModel
    /// </summary>
    public partial class LogPanelViewModel : ObservableObject
    {
        private readonly ILogger<LogPanelViewModel> _logger;
        private readonly StringBuilder _logBuffer = new();
        private const int MAX_LOG_LENGTH = 50000;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<string> _logItems = new();

        [ObservableProperty]
        private int _logEntryCount = 0;

        public LogPanelViewModel(ILogger<LogPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Phase 5統合版LogPanelViewModel を初期化しています");
            
            WriteLog("=== ログパネル初期化完了 ===");
        }

        /// <summary>
        /// ログを書き込み（標準形式）
        /// </summary>
        public void WriteLog(string text)
        {
            WriteLog(DateTime.Now.ToString("HH:mm:ss.fff"), "", text);
        }

        /// <summary>
        /// ログを書き込み（詳細形式）
        /// </summary>
        public void WriteLog(string time, string command, string detail)
        {
            try
            {
                var logEntry = string.IsNullOrEmpty(command) 
                    ? $"[{time}] {detail}"
                    : $"[{time}] {command}: {detail}";
                
                LogItems.Add(logEntry);
                LogEntryCount++;
                
                // ログが多すぎる場合は古い部分を削除
                if (LogItems.Count > 1000)
                {
                    var itemsToRemove = LogItems.Take(500).ToList();
                    foreach (var item in itemsToRemove)
                    {
                        LogItems.Remove(item);
                    }
                    _logger.LogDebug("ログアイテムをトリムしました: {Count}行保持", LogItems.Count);
                }
                
                // 詳細ログは除く
                if (!detail.Contains("PropertyChanged") && !detail.Contains("プロパティ"))
                {
                    _logger.LogDebug("ログエントリ追加: {Entry}", logEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログ書き込み中にエラーが発生しました");
            }
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
            
            if (isRunning)
            {
                WriteLog("=== マクロ実行開始 ===");
            }
            else
            {
                WriteLog("=== マクロ実行終了 ===");
            }
        }

        /// <summary>
        /// ログをクリア
        /// </summary>
        public void Clear()
        {
            try
            {
                _logger.LogDebug("ログを手動クリアします");
                
                LogItems.Clear();
                LogEntryCount = 0;
                
                WriteLog("=== ログクリア ===");
                
                _logger.LogDebug("ログクリアが完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログクリア中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 準備処理
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger.LogDebug("LogPanelViewModel の準備処理を実行します");
                
                // 必要に応じて準備処理を追加
                WriteLog("=== ログパネル準備完了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogPanelViewModel 準備処理中にエラーが発生しました");
            }
        }
    }
}