using System;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Logging;

namespace AutoTool.Logging
{
    public sealed class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly LogMessageService _service;

        public InMemoryLoggerProvider(LogMessageService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new InMemoryLogger(_service, categoryName);
        }

        public void Dispose()
        {
            // nothing to dispose
        }

        private sealed class InMemoryLogger : ILogger
        {
            private readonly LogMessageService _service;
            private readonly string _category;

            public InMemoryLogger(LogMessageService service, string category)
            {
                _service = service;
                _category = category;
            }

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                try
                {
                    var msg = formatter(state, exception);
                    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_category} : {msg}";
                    if (exception != null)
                    {
                        line += $" | {exception.GetType().Name}: {exception.Message}";
                    }
                    _service.AddEntry(line);
                }
                catch
                {
                    // ignore logging errors
                }
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new NullScope();
                public void Dispose() { }
            }
        }
    }
}
