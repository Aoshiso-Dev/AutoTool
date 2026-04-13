using System;
using AutoTool.Infrastructure;
using AutoTool.Core.Ports;

namespace AutoTool.Infrastructure.Implementations
{
public class DelegatingLogWriter : ILogWriter
{
    private readonly AsyncFileLog _asyncFileLog;

    public DelegatingLogWriter(AsyncFileLog asyncFileLog)
    {
        _asyncFileLog = asyncFileLog ?? throw new ArgumentNullException(nameof(asyncFileLog));
    }

    public void Write(params string[] messages)
    {
        _asyncFileLog.Write(messages);
    }

    public void Write(Exception exception)
    {
        _asyncFileLog.Write(exception);
    }
}
}

