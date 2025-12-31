using System.Diagnostics;
using MacroPanels.Command.Interface;

namespace MacroPanels.Command.Commands;

/// <summary>
/// 待機コマンド
/// </summary>
public class WaitCommand : BaseCommand, IWaitCommand
{
    public new IWaitCommandSettings Settings => (IWaitCommandSettings)base.Settings;

    public WaitCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < Settings.Wait)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Wait);

            await Task.Delay(100, cancellationToken);
        }

        RaiseDoingCommand("待機しました。");
        return true;
    }
}
