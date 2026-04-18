using System;
using System.Threading;
using System.Windows.Threading;
using AutoTool.Core.Ports;

namespace AutoTool.Infrastructure.Implementations;

public class DispatcherStatusMessageScheduler : IStatusMessageScheduler
{
    public void Schedule(TimeSpan delay, Action action)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(delay);
            if (await timer.WaitForNextTickAsync(CancellationToken.None).ConfigureAwait(false))
            {
                await dispatcher.InvokeAsync(action);
            }
        });
    }
}
