using AutoTool.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AutoTool.Desktop.Logging;

/// <summary>
/// Host 標準の ILogger 出力を AutoTool の非同期ファイルログへ橋渡しします。
/// </summary>
public sealed class AsyncFileLoggerProvider(AsyncFileLog asyncFileLog) : ILoggerProvider
{
    private readonly AsyncFileLog _asyncFileLog = asyncFileLog ?? throw new ArgumentNullException(nameof(asyncFileLog));

    public ILogger CreateLogger(string categoryName) => new AsyncFileLogger(_asyncFileLog, categoryName);

    public void Dispose()
    {
    }

    private sealed class AsyncFileLogger(AsyncFileLog asyncFileLog, string categoryName) : ILogger
    {
        private readonly AsyncFileLog _asyncFileLog = asyncFileLog;
        private readonly string _categoryName = categoryName;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var fields = new Dictionary<string, object?>
            {
                ["Level"] = logLevel.ToString(),
                ["Category"] = _categoryName,
                ["EventId"] = eventId.Id,
                ["EventName"] = eventId.Name,
                ["Message"] = message
            };

            if (exception is not null)
            {
                fields["Exception"] = exception.GetBaseException().Message;
            }

            _asyncFileLog.WriteStructured("Microsoft.Extensions.Logging", "Log", fields);

            if (exception is not null)
            {
                _asyncFileLog.Write(exception);
            }
        }
    }
}
