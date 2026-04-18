using System;

namespace AutoTool.Application.Ports;

public interface IStatusMessageScheduler
{
    void Schedule(TimeSpan delay, Action action);
}

