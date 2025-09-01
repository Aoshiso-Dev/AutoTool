using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Message;
using CommunityToolkit.Mvvm.Input;
using LogHelper;
using Microsoft.Extensions.Logging;

namespace MacroPanels.ViewModel
{
    public partial class LogPanelViewModel : ObservableObject
    {
        private readonly ILogger<LogPanelViewModel>? _logger;
        private readonly StringBuilder _logBuffer = new();
        private const int MAX_LOG_LENGTH = 50000; // ログの最大文字数

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _log = string.Empty;

        [ObservableProperty]
        private int _logEntryCount = 0;

        // レガシーサポート用コンストラクタ
        public LogPanelViewModel()
        {
            Initialize();
        }

        // DI対応コンストラクタ
        public LogPanelViewModel(ILogger<LogPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("LogPanelViewModel をDI対応で初期化しています");
            
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _logger?.LogDebug("LogPanelViewModel の初期化を開始します");
                
                // ログの初期化
                _logBuffer.Clear();
                Log = string.Empty;
                LogEntryCount = 0;
                
                _logger?.LogDebug("LogPanelViewModel の初期化が完了しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LogPanelViewModel の初期化中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 実行状態を設定
        /// </summary>
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger?.LogDebug("実行状態を設定: {IsRunning}", isRunning);
            
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
        /// 準備処理（ログクリア）
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger?.LogDebug("ログパネルの準備を実行します（ログクリア）");
                
                _logBuffer.Clear();
                Log = string.Empty;
                LogEntryCount = 0;
                
                WriteLog("=== ログクリア ===");
                
                _logger?.LogDebug("ログパネルの準備が完了しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ログパネルの準備中にエラーが発生しました");
            }
        }

        /// <summary>
        /// シンプルなログ出力
        /// </summary>
        public void WriteLog(string text)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {text}{Environment.NewLine}";
                
                AppendLogEntry(logEntry);
                
                _logger?.LogTrace("ログエントリを追加: {Text}", text);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ログ出力中にエラーが発生しました: {Text}", text);
            }
        }

        /// <summary>
        /// 構造化されたログ出力
        /// </summary>
        public void WriteLog(string lineNumber, string commandName, string detail)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var formattedLog = $"[{timestamp}] {lineNumber.PadRight(4)} {commandName.PadRight(20)} {detail}{Environment.NewLine}";
                
                AppendLogEntry(formattedLog);
                
                _logger?.LogTrace("構造化ログエントリを追加: Line={LineNumber}, Command={CommandName}, Detail={Detail}", 
                    lineNumber, commandName, detail);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "構造化ログ出力中にエラーが発生しました: Line={LineNumber}, Command={CommandName}", 
                    lineNumber, commandName);
            }
        }

        /// <summary>
        /// ログエントリをバッファに追加（バッファサイズ管理付き）
        /// </summary>
        private void AppendLogEntry(string logEntry)
        {
            try
            {
                _logBuffer.Append(logEntry);
                LogEntryCount++;
                
                // バッファサイズ管理
                if (_logBuffer.Length > MAX_LOG_LENGTH)
                {
                    var content = _logBuffer.ToString();
                    var halfLength = content.Length / 2;
                    var newlineIndex = content.IndexOf(Environment.NewLine, halfLength);
                    
                    if (newlineIndex > 0)
                    {
                        var trimmedContent = content.Substring(newlineIndex + Environment.NewLine.Length);
                        _logBuffer.Clear();
                        _logBuffer.Append(trimmedContent);
                        
                        _logger?.LogDebug("ログバッファを整理しました: {OldLength} -> {NewLength}", 
                            content.Length, _logBuffer.Length);
                    }
                }
                
                // UIに反映
                Log = _logBuffer.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ログエントリの追加中にエラーが発生しました");
            }
        }

        /// <summary>
        /// ログをクリア
        /// </summary>
        [RelayCommand]
        public void ClearLog()
        {
            try
            {
                _logger?.LogDebug("ログを手動でクリアします");
                
                _logBuffer.Clear();
                Log = string.Empty;
                LogEntryCount = 0;
                
                WriteLog("=== ログ手動クリア ===");
                
                _logger?.LogInformation("ログがクリアされました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ログクリア中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 最近のエラー行を取得（実行時エラー表示用）
        /// </summary>
        /// <param name="maxLines">取得する最大行数</param>
        /// <returns>最近のエラー行のリスト</returns>
        public List<string> GetRecentErrorLines(int maxLines = 5)
        {
            try
            {
                var errorLines = new List<string>();
                var logContent = _logBuffer.ToString();
                
                if (string.IsNullOrEmpty(logContent))
                    return errorLines;
                
                var lines = logContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                
                // 後ろから検索してエラー行を取得
                for (int i = lines.Length - 1; i >= 0 && errorLines.Count < maxLines; i--)
                {
                    var line = lines[i];
                    
                    // エラーを示すキーワードを含む行を検索
                    if (line.Contains("❌") || 
                        line.Contains("エラー") || 
                        line.Contains("失敗") || 
                        line.Contains("見つかりません") ||
                        line.Contains("Exception") ||
                        line.Contains("Error"))
                    {
                        errorLines.Add(line);
                    }
                }
                
                // 順序を元に戻す（時系列順）
                errorLines.Reverse();
                
                _logger?.LogDebug("最近のエラー行を取得: {Count}件", errorLines.Count);
                return errorLines;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "最近のエラー行取得中にエラーが発生しました");
                return new List<string>();
            }
        }

        /// <summary>
        /// ログ統計情報を取得
        /// </summary>
        public string GetLogStatistics()
        {
            try
            {
                var stats = $"エントリ数: {LogEntryCount}, 文字数: {_logBuffer.Length}, " +
                           $"最大サイズ: {MAX_LOG_LENGTH:N0}文字";
                
                _logger?.LogDebug("ログ統計: {Statistics}", stats);
                return stats;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ログ統計取得中にエラーが発生しました");
                return "統計取得エラー";
            }
        }
    }
}