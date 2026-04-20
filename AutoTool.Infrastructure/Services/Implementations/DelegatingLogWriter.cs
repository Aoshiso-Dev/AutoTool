using System;
using System.Collections.Generic;
using AutoTool.Application.Ports;

namespace AutoTool.Infrastructure.Implementations;

/// <summary>
/// `ILogWriter` 呼び出しを `AsyncFileLog` へ委譲するアダプタ実装です。
/// </summary>
public class DelegatingLogWriter(AutoTool.Infrastructure.AsyncFileLog asyncFileLog) : ILogWriter
{
    private readonly AutoTool.Infrastructure.AsyncFileLog _asyncFileLog = EnsureNotNull(asyncFileLog);

    /// <summary>
    /// 文字列メッセージを非同期ログへ書き込みます。
    /// </summary>
    public void Write(params string[] messages)
    {
        _asyncFileLog.Write(messages);
    }

    /// <summary>
    /// 構造化ログを非同期ログへ書き込みます。
    /// </summary>
    public void WriteStructured(string category, string eventName, IReadOnlyDictionary<string, object?> fields)
    {
        _asyncFileLog.WriteStructured(category, eventName, fields);
    }

    /// <summary>
    /// 例外情報を非同期ログへ書き込みます。
    /// </summary>
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
