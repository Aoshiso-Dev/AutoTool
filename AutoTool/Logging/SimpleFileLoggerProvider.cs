using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AutoTool.Logging
{
    /// <summary>
    /// ファイルロガー設定
    /// </summary>
    internal sealed class SimpleFileLoggerOptions
    {
        /// <summary> フラッシュ間隔(ms) デフォルト 2000 </summary>
        public int FlushIntervalMs { get; set; } = 2000;
        /// <summary> バッファしきい値(文字数) デフォルト 4096 </summary>
        public int FlushThreshold { get; set; } = 4096;
        /// <summary> エラー以上は即時フラッシュ </summary>
        public bool FlushOnError { get; set; } = true;
        /// <summary> 1ファイル最大サイズ(bytes) 超えたらローテーション (0=無効) </summary>
        public long MaxFileBytes { get; set; } = 5_000_000; // 5MB
        /// <summary> 保持世代数 (日/サイズローテーション共通) 0=無制限 </summary>
        public int RetainFileCount { get; set; } = 7;
        /// <summary> 例外詳細を含める </summary>
        public bool IncludeExceptionStack { get; set; } = false;
    }

    /// <summary>
    /// シンプルなファイルロガー (日付/サイズローテーション + バッファリング)
    /// </summary>
    internal sealed class SimpleFileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly string _filePrefix;
        private readonly System.Threading.Timer _flushTimer;
        private readonly object _sync = new();
        private StreamWriter? _writer;
        private DateTime _currentDate = DateTime.Today;
        private bool _disposed;
        private readonly StringBuilder _buffer = new();
        private readonly SimpleFileLoggerOptions _options;
        private string _currentPath = string.Empty;
        private int _sameDaySequence = 0; // サイズローテ用連番

        public SimpleFileLoggerProvider(string logDirectory, string filePrefix = "app", SimpleFileLoggerOptions? options = null)
        {
            _logDirectory = logDirectory;
            _filePrefix = filePrefix;
            _options = options ?? new SimpleFileLoggerOptions();
            Directory.CreateDirectory(_logDirectory);
            OpenWriter(forceSequenceReset: true);
            _flushTimer = new System.Threading.Timer(_ => Flush(false), null, _options.FlushIntervalMs, _options.FlushIntervalMs);
        }

        private void OpenWriter(bool forceSequenceReset = false)
        {
            _currentDate = DateTime.Today;
            if (forceSequenceReset) _sameDaySequence = 0;
            _currentPath = BuildLogFilePath();
            _writer = new StreamWriter(new FileStream(_currentPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete), Encoding.UTF8)
            {
                AutoFlush = false
            };
            CleanupOldFiles();
        }

        private string BuildLogFilePath()
        {
            // ファイル名: prefix-YYYYMMDD[-NN].log
            string baseName = $"{_filePrefix}-{_currentDate:yyyyMMdd}";
            string name = _sameDaySequence == 0 ? baseName + ".log" : $"{baseName}-{_sameDaySequence:D2}.log";
            return Path.Combine(_logDirectory, name);
        }

        private void RotateIfNeeded_NoLock()
        {
            // 日付変わり
            if (DateTime.Today != _currentDate)
            {
                Flush(true);
                _writer?.Dispose();
                OpenWriter(forceSequenceReset: true);
                return;
            }
            // サイズ
            if (_options.MaxFileBytes > 0 && _writer != null)
            {
                try
                {
                    var len = (_writer.BaseStream?.Length) ?? 0;
                    if (len >= _options.MaxFileBytes)
                    {
                        Flush(true);
                        _writer.Dispose();
                        _sameDaySequence++;
                        OpenWriter();
                    }
                }
                catch { }
            }
        }

        public ILogger CreateLogger(string categoryName) => new SimpleFileLogger(this, categoryName);

        private sealed class SimpleFileLogger : ILogger
        {
            private readonly SimpleFileLoggerProvider _provider;
            private readonly string _category;
            public SimpleFileLogger(SimpleFileLoggerProvider provider, string category)
            {
                _provider = provider;
                _category = category;
            }
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                var msg = formatter(state, exception);
                _provider.WriteLine(_category, logLevel, msg, exception);
                if (_provider._options.FlushOnError && (logLevel >= LogLevel.Error))
                {
                    _provider.Flush(true);
                }
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }

        private void WriteLine(string category, LogLevel level, string message, Exception? ex)
        {
            try
            {
                lock (_sync)
                {
                    if (_disposed) return;
                    RotateIfNeeded_NoLock();
                    var line = new StringBuilder()
                        .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                        .Append(' ') .Append('[').Append(level).Append(']')
                        .Append(' ') .Append(category) .Append(" : ")
                        .Append(message);
                    if (ex != null)
                    {
                        line.Append(" | ").Append(ex.GetType().Name).Append(':').Append(ex.Message);
                        if (_options.IncludeExceptionStack && ex.StackTrace != null)
                            line.Append("\n").Append(ex.StackTrace);
                    }
                    _buffer.AppendLine(line.ToString());
                    if (_buffer.Length >= _options.FlushThreshold)
                    {
                        Flush(false);
                    }
                }
            }
            catch { /* ignore */ }
        }

        private void Flush(bool force)
        {
            lock (_sync)
            {
                if (_disposed) return;
                if (_writer == null) return;
                if (_buffer.Length == 0 && !force) return;
                try
                {
                    _writer.Write(_buffer.ToString());
                    _buffer.Clear();
                    _writer.Flush();
                }
                catch { }
            }
        }

        private void CleanupOldFiles()
        {
            try
            {
                if (_options.RetainFileCount <= 0) return;
                var pattern = $"{_filePrefix}-"; // prefix-
                var files = Directory.GetFiles(_logDirectory, _filePrefix + "-*.log", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => File.GetCreationTimeUtc(f))
                    .ToList();
                if (files.Count <= _options.RetainFileCount) return;
                foreach (var old in files.Skip(_options.RetainFileCount))
                {
                    try { File.Delete(old); } catch { }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _flushTimer.Dispose();
            lock (_sync)
            {
                try
                {
                    Flush(true);
                    _writer?.Dispose();
                }
                catch { }
            }
        }
    }

    internal static class SimpleFileLoggerExtensions
    {
        public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder, string? directory = null, string filePrefix = "app", Action<SimpleFileLoggerOptions>? configure = null)
        {
            directory ??= Path.Combine(AppContext.BaseDirectory, "Logs");
            var opt = new SimpleFileLoggerOptions();
            configure?.Invoke(opt);
            var provider = new SimpleFileLoggerProvider(directory, filePrefix, opt);
            builder.AddProvider(provider);
            return builder;
        }
    }
}
