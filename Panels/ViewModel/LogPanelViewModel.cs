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
        private readonly ILogger<LogPanelViewModel> _logger;
        private readonly StringBuilder _logBuffer = new();
        private const int MAX_LOG_LENGTH = 50000; // ログの最大文字数

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _log = string.Empty;

        [ObservableProperty]
        private int _logEntryCount = 0;

        /// <summary>
        /// DI対応コンストラクタ
        /// </summary>
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
                _logger.LogDebug("LogPanelViewModel の初期化を開始します");
                
                // ログの初期化
                _logBuffer.Clear();
                Log = string.Empty;
                LogEntryCount = 0;
                
                _logger.LogDebug("LogPanelViewModel の初期化が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogPanelViewModel の初期化中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 実行状態を設定
        /// </summary>
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
        /// 準備処理（ログクリア）
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger.LogDebug("ログパネルの準備を実行します（ログクリア）");
                
                _logBuffer.Clear();
                Log = string.Empty;
                LogEntryCount = 0;
                
                WriteLog("=== ログクリア ===");
                
                _logger.LogDebug("ログパネルの準備が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログパネルの準備中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// ログをクリア
        /// </summary>
        [RelayCommand]
        public void Clear()
        {
            try
            {
                _logger.LogDebug("ログを手動クリアします");
                
                _logBuffer.Clear();
                Log = string.Empty;
                LogEntryCount = 0;
                
                _logger.LogDebug("ログクリアが完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログクリア中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// ログを書き込み（標準形式）
        /// </summary>
        /// <param name="text">ログテキスト</param>
        public void WriteLog(string text)
        {
            WriteLog(DateTime.Now.ToString("HH:mm:ss.fff"), "", text);
        }

        /// <summary>
        /// ログを書き込み（詳細形式）
        /// </summary>
        /// <param name="time">時刻</param>
        /// <param name="command">コマンド名</param>
        /// <param name="detail">詳細</param>
        public void WriteLog(string time, string command, string detail)
        {
            try
            {
                var logEntry = string.IsNullOrEmpty(command) 
                    ? $"[{time}] {detail}"
                    : $"[{time}] {command}: {detail}";
                
                _logBuffer.AppendLine(logEntry);
                LogEntryCount++;
                
                // ログが長すぎる場合は古い部分を削除
                if (_logBuffer.Length > MAX_LOG_LENGTH)
                {
                    var lines = _logBuffer.ToString().Split('\n');
                    var keepLines = lines.Skip(lines.Length / 2).ToArray();
                    _logBuffer.Clear();
                    _logBuffer.AppendLine(string.Join("\n", keepLines));
                    _logger.LogDebug("ログバッファをトリムしました: {Lines}行保持", keepLines.Length);
                }
                
                Log = _logBuffer.ToString();
                
                // 詳細ログは除く
                if (!detail.Contains("PropertyChanged") && !detail.Contains("プロパティ"))
                {
                    _logger.LogDebug("ログエントリ追加: {Entry}", logEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログ書き込み中にエラーが発生しました: {Detail}", detail);
            }
        }

        /// <summary>
        /// 最近のエラーログ行を取得
        /// </summary>
        /// <param name="count">取得する行数（デフォルト5行）</param>
        /// <returns>エラーを含むログ行のリスト</returns>
        public List<string> GetRecentErrorLines(int count = 5)
        {
            try
            {
                var lines = Log.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var errorLines = lines
                    .Where(line => line.Contains("❌") || line.Contains("エラー") || line.Contains("Error") || 
                                  line.Contains("失敗") || line.Contains("Exception"))
                    .TakeLast(count)
                    .ToList();
                    
                _logger.LogDebug("最近のエラーログ取得: {Count}行", errorLines.Count);
                return errorLines;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "エラーログ取得中にエラーが発生しました");
                return new List<string>();
            }
        }

        /// <summary>
        /// ログをファイルに保存
        /// </summary>
        /// <param name="filePath">保存先ファイルパス</param>
        public void SaveToFile(string filePath)
        {
            try
            {
                _logger.LogInformation("ログをファイルに保存します: {FilePath}", filePath);
                
                System.IO.File.WriteAllText(filePath, Log);
                
                _logger.LogInformation("ログファイル保存が完了しました: {EntryCount}件", LogEntryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログファイル保存中にエラーが発生しました: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// ログの統計情報を取得
        /// </summary>
        public (int TotalLines, int ErrorLines, int WarningLines, int InfoLines) GetLogStatistics()
        {
            try
            {
                var lines = Log.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var totalLines = lines.Length;
                var errorLines = lines.Count(line => line.Contains("❌") || line.Contains("エラー") || line.Contains("Error"));
                var warningLines = lines.Count(line => line.Contains("⚠️") || line.Contains("警告") || line.Contains("Warning"));
                var infoLines = lines.Count(line => line.Contains("ℹ️") || line.Contains("情報") || line.Contains("Info"));
                
                _logger.LogDebug("ログ統計: Total={Total}, Error={Error}, Warning={Warning}, Info={Info}", 
                    totalLines, errorLines, warningLines, infoLines);
                    
                return (totalLines, errorLines, warningLines, infoLines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログ統計取得中にエラーが発生しました");
                return (0, 0, 0, 0);
            }
        }
    }
}