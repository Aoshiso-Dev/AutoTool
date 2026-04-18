using System;
using System.Collections.Generic;

namespace AutoTool.Core.Ports;

public interface ILogWriter
{
    void Write(params string[] messages);
    void WriteStructured(string category, string eventName, IReadOnlyDictionary<string, object?> fields);
    void Write(Exception exception);
}
