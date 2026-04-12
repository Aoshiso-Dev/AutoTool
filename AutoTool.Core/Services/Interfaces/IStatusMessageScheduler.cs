using System;

namespace AutoTool.Services.Interfaces
{
    public interface IStatusMessageScheduler
    {
        void Schedule(TimeSpan delay, Action action);
    }
}
