using System;

namespace AutoTool.Core.Ports;

public interface IStatusMessageScheduler
{
    void Schedule(TimeSpan delay, Action action);
}

