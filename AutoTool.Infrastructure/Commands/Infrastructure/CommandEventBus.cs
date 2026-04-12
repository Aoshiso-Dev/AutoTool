using AutoTool.Commands.Services;
using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// ICommandEventBus の既定実装
/// </summary>
public sealed class CommandEventBus : ICommandEventBus
{
    public event EventHandler<CommandEventArgs>? Started;
    public event EventHandler<CommandEventArgs>? Finished;
    public event EventHandler<CommandLogEventArgs>? Doing;
    public event EventHandler<CommandProgressEventArgs>? ProgressUpdated;

    public void PublishStarted(ICommand command)
    {
        Started?.Invoke(this, new CommandEventArgs(command));
    }

    public void PublishFinished(ICommand command)
    {
        Finished?.Invoke(this, new CommandEventArgs(command));
    }

    public void PublishDoing(ICommand command, string detail)
    {
        Doing?.Invoke(this, new CommandLogEventArgs(command, detail));
    }

    public void PublishProgress(ICommand command, int progress)
    {
        ProgressUpdated?.Invoke(this, new CommandProgressEventArgs(command, progress));
    }
}
