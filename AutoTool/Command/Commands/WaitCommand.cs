using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(WaitCommand), typeof(WaitCommand))]
    public class WaitCommand : BaseCommand
    {
        [Category("基本設定"), DisplayName("待機時間(ms)")]
        public int Wait { get; set; } = 1000;

        public WaitCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "待機";
        }

        protected override void ValidateSettings()
        {
            if (Wait < 0) throw new ArgumentException("待機時間は0以上を指定してください");
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Wait <= 0)
            {
                ReportProgress(1, 1);
                LogMessage("待機時間が0のため即終了します");
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            var totalWaitMs = Wait;
            int lastReported = -1;

            LogMessage($"待機を開始します: {totalWaitMs}ms");

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogMessage("待機がキャンセルされました");
                    return false;
                }

                var elapsed = stopwatch.ElapsedMilliseconds;
                if (elapsed >= totalWaitMs)
                {
                    ReportProgress(totalWaitMs, totalWaitMs);
                    LogMessage("待機が完了しました");
                    return true;
                }

                var progress = (int)Math.Clamp((elapsed / (double)totalWaitMs) * 100, 0, 100);
                if (progress != lastReported)
                {
                    ReportProgress(elapsed, totalWaitMs);
                    lastReported = progress;

                    var remaining = Math.Max(0, totalWaitMs - (int)elapsed);
                    LogMessage($"待機中: {progress}% (残り {remaining}ms)");
                }

                var nextSlice = (int)Math.Min(100, totalWaitMs - elapsed);
                await Task.Delay(nextSlice, cancellationToken);
            }
        }
    }
}
