using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Commands.Services;

public enum CommandEventKind
{
    Started,
    Finished,
    Doing,
    ProgressUpdated
}

public abstract record CommandLogPayload;

public sealed record ProcessOutputLogPayload(
    bool IsError,
    string Text,
    DateTimeOffset Timestamp) : CommandLogPayload;

public sealed record CommandBusEvent(
    CommandEventKind Kind,
    ICommand Command,
    string Detail = "",
    int Progress = 0,
    CommandLogPayload? Payload = null);

public interface ICommandEventBus
{
    event EventHandler<CommandEventArgs>? Started;
    event EventHandler<CommandEventArgs>? Finished;
    event EventHandler<CommandLogEventArgs>? Doing;
    event EventHandler<CommandProgressEventArgs>? ProgressUpdated;

    void PublishStarted(ICommand command);
    void PublishFinished(ICommand command);
    void PublishDoing(ICommand command, string detail);
    void PublishDoing(ICommand command, string detail, CommandLogPayload payload);
    void PublishProgress(ICommand command, int progress);
    long DroppedEventCount { get; }
    int SubscriberCount { get; }
    IAsyncEnumerable<CommandBusEvent> ReadEventsAsync(CancellationToken cancellationToken = default);
}

public class CommandEventArgs(ICommand command) : EventArgs
{
    public ICommand Command { get; } = EnsureNotNull(command);

    private static ICommand EnsureNotNull(ICommand value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}

public sealed class CommandLogEventArgs : CommandEventArgs
{
    public CommandLogEventArgs(ICommand command, string detail, CommandLogPayload? payload = null) : base(command)
    {
        Detail = detail ?? string.Empty;
        Payload = payload;
    }

    public string Detail { get; }
    public CommandLogPayload? Payload { get; }
}

public sealed class CommandProgressEventArgs : CommandEventArgs
{
    public CommandProgressEventArgs(ICommand command, int progress) : base(command)
    {
        Progress = progress;
    }

    public int Progress { get; }
}
