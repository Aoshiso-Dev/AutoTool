using System.Threading.Channels;
using System.Collections.Generic;
using AutoTool.Commands.Threading;
using AutoTool.Automation.Runtime.Diagnostics;
using System.IO;

namespace AutoTool.Infrastructure;

/// <summary>
/// ログメッセージを受け取り、指定先へ非同期で書き込みます。
/// </summary>
public sealed class AsyncFileLog : IDisposable, IAsyncDisposable
{
    private readonly Channel<string> _logChannel;
    private readonly ChannelWriter<string> _writer;
    private readonly ChannelReader<string> _reader;
    private readonly string _logDir;
    private readonly string _logPath;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processingTask;
    private readonly TimeProvider _timeProvider;
    private volatile bool _disposed;
    private const int FlushBatchSize = 32;

    public AsyncFileLog(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? @"C:\";
        _logDir = Path.Combine(appDir, "Logs");
        _logPath = Path.Combine(_logDir, $"{_timeProvider.GetLocalNow():yyyy-MM-dd_HH-mm-ss}.log");

        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _logChannel = Channel.CreateBounded<string>(options);
        _writer = _logChannel.Writer;
        _reader = _logChannel.Reader;

        _cancellationTokenSource = new();
        _processingTask = Task.Run(() => ProcessLogQueueAsync(_cancellationTokenSource.Token));
    }

    public void Write(params string[] messages)
    {
        if (_disposed || _reader.Completion.IsCompleted)
        {
            return;
        }

        try
        {
            var formattedMessage = $"[{_timeProvider.GetLocalNow():yyyy-MM-dd HH:mm:ss}] " +
                                   string.Join(" ", messages.Select(m => (m ?? string.Empty).PadRight(20)));

            if (!_writer.TryWrite(formattedMessage))
            {
                _ = EnqueueAsync(formattedMessage);
            }
        }
        catch (InvalidOperationException)
        {
            // すでに完了済みのライターへ書き込んだ場合は無視します。
        }
    }

    public void Write(Exception ex)
    {
        Write($"例外: {ExceptionDetailsFormatter.FormatDetailed(ex)}");
        Write($"スタックトレース: {ex.StackTrace}");
    }

    public void WriteStructured(string category, string eventName, IReadOnlyDictionary<string, object?> fields)
    {
        if (fields is null)
        {
            Write("STRUCT", $"Category={category}", $"Event={eventName}");
            return;
        }

        List<string> segments =
        [
            "STRUCT",
            $"Category={category}",
            $"Event={eventName}"
        ];

        foreach (var kvp in fields)
        {
            segments.Add($"{kvp.Key}={FormatStructuredValue(kvp.Value)}");
        }

        Write(segments.ToArray());
    }

    private async Task ProcessLogQueueAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_logDir))
        {
            Directory.CreateDirectory(_logDir);
        }

        try
        {
            await using var fileStream = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096);
            await using var streamWriter = new StreamWriter(fileStream);

            List<string> bufferedMessages = new(FlushBatchSize);

            await foreach (var logMessage in _reader.ReadAllAsync().ConfigureAwaitFalse(cancellationToken))
            {
                try
                {
                    bufferedMessages.Add(logMessage);
                    if (bufferedMessages.Count >= FlushBatchSize)
                    {
                        await FlushBatchAsync(streamWriter, bufferedMessages, cancellationToken);
                    }
                }
                catch (IOException ioEx)
                {
                    WriteFallbackError("ProcessLogQueue", "FlushIOException", ioEx);
                }
                catch (Exception ex)
                {
                    WriteFallbackError("ProcessLogQueue", "FlushUnexpectedError", ex);
                }
            }

            if (bufferedMessages.Count > 0)
            {
                await FlushBatchAsync(streamWriter, bufferedMessages, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // シャットダウン時の想定内キャンセルです。
        }
        catch (Exception ex)
        {
            WriteFallbackError("ProcessLogQueue", "UnhandledException", ex);
        }
    }

    public void Dispose()
    {
        try
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            WriteFallbackError("Dispose", "SyncDisposeError", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _writer.Complete();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completed = await Task.WhenAny(_processingTask, timeoutTask);
            if (completed != _processingTask)
            {
                _cancellationTokenSource.Cancel();
            }

            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            // シャットダウン時の想定内キャンセルです。
        }
        catch (Exception ex)
        {
            WriteFallbackError("DisposeAsync", "AsyncDisposeError", ex);
        }
        finally
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    private static async Task FlushBatchAsync(StreamWriter writer, List<string> messages, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            await writer.WriteLineAsync(message);
        }

        messages.Clear();
        await writer.FlushAsync(cancellationToken);
    }

    private async Task EnqueueAsync(string message)
    {
        try
        {
            await _writer.WriteAsync(message);
        }
        catch (Exception)
        {
            // 終了処理中はベストエフォートで、キュー投入失敗は無視します。
        }
    }

    private static string FormatStructuredValue(object? value)
    {
        if (value is null)
        {
            return "（null）";
        }

        return value.ToString()?.Replace("\r", "\\r").Replace("\n", "\\n") ?? string.Empty;
    }

    private static void WriteFallbackError(string stage, string eventName, Exception exception)
    {
        _ = stage;
        _ = eventName;
        _ = exception;
    }
}

