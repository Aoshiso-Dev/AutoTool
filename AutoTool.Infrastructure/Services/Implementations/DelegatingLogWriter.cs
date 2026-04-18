using System;
using System.Collections.Generic;
using AutoTool.Core.Ports;

namespace AutoTool.Infrastructure.Implementations;

public class DelegatingLogWriter(AutoTool.Infrastructure.AsyncFileLog asyncFileLog) : ILogWriter
{
    private readonly AutoTool.Infrastructure.AsyncFileLog _asyncFileLog = EnsureNotNull(asyncFileLog);

    public void Write(params string[] messages)
    {
        _asyncFileLog.Write(messages);
    }

    public void WriteStructured(string category, string eventName, IReadOnlyDictionary<string, object?> fields)
    {
        _asyncFileLog.WriteStructured(category, eventName, fields);
    }

    public void Write(Exception exception)
    {
        _asyncFileLog.Write(exception);
    }

    private static AutoTool.Infrastructure.AsyncFileLog EnsureNotNull(AutoTool.Infrastructure.AsyncFileLog value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}
