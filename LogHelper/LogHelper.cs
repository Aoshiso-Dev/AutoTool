using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LogHelper
{
    public class Log
    {
        private static readonly Lazy<Log> _instance = new Lazy<Log>(() => new Log());
        public static Log Instance => _instance.Value;

        private readonly BlockingCollection<string> _logQueue = new BlockingCollection<string>();
        private readonly string _logDir;
        private string _logPath;
        private CancellationTokenSource _cancellationTokenSource;

        public Log()
        {
            var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? @"C:\";
            _logDir = Path.Combine(appDir, "Logs");
            UpdateLogPath();

            // 非同期でログを書き込むタスクを開始
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ProcessLogQueue(_cancellationTokenSource.Token));
        }

        private void UpdateLogPath()
        {
            _logPath = Path.Combine(_logDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        }


        public void Write(params string[] messages)
        {
            // 各メッセージを指定の長さにパディングして結合
            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                      string.Join(" ", messages.Select(m => m.PadRight(20)));

            // ログメッセージをキューに追加
            _logQueue.Add(formattedMessage);
        }

        public void Write(Exception ex)
        {
            Write($"Exception: {ex.Message}");
            Write($"StackTrace: {ex.StackTrace}");
        }

        private void ProcessLogQueue(CancellationToken cancellationToken)
        {
            // ログディレクトリを作成
            if (!Directory.Exists(_logDir))
            {
                Directory.CreateDirectory(_logDir);
            }

            foreach (var logMessage in _logQueue.GetConsumingEnumerable(cancellationToken))
            {
                try
                {
                    //UpdateLogPath();
                    using (var sw = new StreamWriter(new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        sw.WriteLine(logMessage);
                    }
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"ログの書き込みに失敗しました: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ログの書き込み中にエラーが発生しました: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            // ログキューを閉じてタスクを終了
            _cancellationTokenSource.Cancel();
            _logQueue.CompleteAdding();
            _cancellationTokenSource.Dispose();
        }
    }
}
