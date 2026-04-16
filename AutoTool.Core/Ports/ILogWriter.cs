using System;

namespace AutoTool.Core.Ports;

public interface ILogWriter
{
    void Write(params string[] messages);
    void Write(Exception exception);
}

