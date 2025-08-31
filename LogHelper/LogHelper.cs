using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace LogHelper
{
    public sealed class Log : IDisposable
    {
        private static readonly Lazy<Log> _instance = new(() => new Log());
        public static Log Instance => _instance.Value;

        private readonly Channel<string> _logChannel;
        private readonly ChannelWriter<string> _writer;
        private readonly ChannelReader<string> _reader;
        private readonly string _logDir;
        private readonly string _logPath;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;
        private volatile bool _disposed = false;

        public Log()
        {
            var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? @"C:\";
            _logDir = Path.Combine(appDir, "Logs");
            _logPath = Path.Combine(_logDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

            // Channel使用でより効率的な非同期処理
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _logChannel = Channel.CreateBounded<string>(options);
            _writer = _logChannel.Writer;
            _reader = _logChannel.Reader;

            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessLogQueueAsync(_cancellationTokenSource.Token));
        }

        public void Write(params string[] messages)
        {
            if (_disposed) return;

            try
            {
                // 各メッセージを指定の長さにパディングして結合
                var formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                      string.Join(" ", messages.Select(m => m.PadRight(20)));

                // 非ブロッキングで書き込み試行
                if (!_writer.TryWrite(formattedMessage))
                {
                    // チャネルが満杯の場合は古いメッセージを破棄
                    Console.WriteLine("Log queue is full, dropping message");
                }
            }
            catch (InvalidOperationException)
            {
                // Writer が閉じられている場合は無視
            }
        }

        public void Write(Exception ex)
        {
            Write($"Exception: {ex.Message}");
            Write($"StackTrace: {ex.StackTrace}");
        }

        private async Task ProcessLogQueueAsync(CancellationToken cancellationToken)
        {
            // ログディレクトリを作成
            if (!Directory.Exists(_logDir))
            {
                Directory.CreateDirectory(_logDir);
            }

            try
            {
                // FileStreamを再利用してパフォーマンス向上
                await using var fileStream = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4096);
                await using var streamWriter = new StreamWriter(fileStream);

                await foreach (var logMessage in _reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        await streamWriter.WriteLineAsync(logMessage);
                        await streamWriter.FlushAsync(); // 即座にディスクに書き込み
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
            catch (OperationCanceledException)
            {
                // 正常なキャンセル
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ログ処理でエラーが発生しました: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Writerを閉じてチャネルを完了
                _writer.Complete();
                
                // 処理タスクの完了を待機（タイムアウト付き）
                _processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log dispose error: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
