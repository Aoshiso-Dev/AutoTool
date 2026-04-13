using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Commands.Services;

/// <summary>
/// コマンド実行イベントを配信するイベントバス
/// </summary>
public interface ICommandEventBus
{
    event EventHandler<CommandEventArgs>? Started;
    event EventHandler<CommandEventArgs>? Finished;
    event EventHandler<CommandLogEventArgs>? Doing;
    event EventHandler<CommandProgressEventArgs>? ProgressUpdated;

    void PublishStarted(ICommand command);
    void PublishFinished(ICommand command);
    void PublishDoing(ICommand command, string detail);
    void PublishProgress(ICommand command, int progress);
}

public class CommandEventArgs : EventArgs
{
    public CommandEventArgs(ICommand command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public ICommand Command { get; }
}

public sealed class CommandLogEventArgs : CommandEventArgs
{
    public CommandLogEventArgs(ICommand command, string detail) : base(command)
    {
        Detail = detail ?? string.Empty;
    }

    public string Detail { get; }
}

public sealed class CommandProgressEventArgs : CommandEventArgs
{
    public CommandProgressEventArgs(ICommand command, int progress) : base(command)
    {
        Progress = progress;
    }

    public int Progress { get; }
}
