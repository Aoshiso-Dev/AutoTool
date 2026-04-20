using AutoTool.Application.Ports;
using System.Windows.Threading;

namespace AutoTool.Desktop.Services;

/// <summary>
/// 指定遅延後に UI スレッドで処理を実行するスケジューラです。
/// </summary>
public class DispatcherStatusMessageScheduler : IStatusMessageScheduler
{
    /// <summary>
    /// 遅延後に `action` を 1 回だけ実行します。
    /// </summary>
    public void Schedule(TimeSpan delay, Action action)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        _ = ScheduleOnceAsync(delay, action, dispatcher);
    }

    /// <summary>
    /// `PeriodicTimer` を 1 ティックだけ利用し、UI Dispatcher 上で処理を実行します。
    /// </summary>
    private static async Task ScheduleOnceAsync(TimeSpan delay, Action action, Dispatcher dispatcher)
    {
        using var timer = new PeriodicTimer(delay);
        if (await timer.WaitForNextTickAsync(CancellationToken.None).ConfigureAwait(false))
        {
            await dispatcher.InvokeAsync(action);
        }
    }
}
