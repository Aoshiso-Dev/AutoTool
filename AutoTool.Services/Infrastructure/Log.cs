using System.Threading.Channels;
using System.IO;

namespace AutoTool.Infrastructure;

public sealed class Log : IDisposable
{
    private static readonly Lazy<Log> InstanceFactory = new(() => new Log());
    public static Log Instance => InstanceFactory.Value;

    private readonly Channel<string> _logChannel;
    private readonly ChannelWriter<string> _writer;
    private readonly ChannelReader<string> _reader;
    private readonly string _logDir;
    private readonly string _logPath;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processingTask;
    private volatile bool _disposed;

    public Log()
    {
        var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? @"C:\";
        _logDir = Path.Combine(appDir, "Logs");
        _logPath = Path.Combine(_logDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

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
        if (_disposed)
        {
            return;
        }

        try
        {
            var formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                   string.Join(" ", messages.Select(m => m.PadRight(20)));

            _writer.TryWrite(formattedMessage);
        }
        catch (InvalidOperationException)
        {
            // ignore when writer is already completed
        }
    }

    public void Write(Exception ex)
    {
        Write($"Exception: {ex.Message}");
        Write($"StackTrace: {ex.StackTrace}");
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

            await foreach (var logMessage in _reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await streamWriter.WriteLineAsync(logMessage);
                    await streamWriter.FlushAsync(cancellationToken);
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"ログの書き込みに失敗しました: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ログ処理でエラーが発生しました: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal on shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ログ処理でエラーが発生しました: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _writer.Complete();
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
