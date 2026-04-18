using System;
using System.Threading;
using System.Windows.Threading;
using AutoTool.Application.Ports;

namespace AutoTool.Infrastructure.Implementations;

public class DispatcherStatusMessageScheduler : IStatusMessageScheduler
{
    public void Schedule(TimeSpan delay, Action action)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        _ = ScheduleOnceAsync(delay, action, dispatcher);
    }

    private static async Task ScheduleOnceAsync(TimeSpan delay, Action action, Dispatcher dispatcher)
    {
        using var timer = new PeriodicTimer(delay);
        if (await timer.WaitForNextTickAsync(CancellationToken.None).ConfigureAwait(false))
        {
            await dispatcher.InvokeAsync(action);
        }
    }
}
