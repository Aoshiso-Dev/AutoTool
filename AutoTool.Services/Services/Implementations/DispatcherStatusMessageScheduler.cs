using System;
using System.Windows.Threading;
using AutoTool.Services.Interfaces;

namespace AutoTool.Services.Implementations
{
    public class DispatcherStatusMessageScheduler : IStatusMessageScheduler
    {
        public void Schedule(TimeSpan delay, Action action)
        {
            var timer = new DispatcherTimer { Interval = delay };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                action();
            };
            timer.Start();
        }
    }
}
