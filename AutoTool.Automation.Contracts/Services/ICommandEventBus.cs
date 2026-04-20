using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Commands.Services;

/// <summary>
/// コマンドイベント種別です。
/// </summary>
public enum CommandEventKind
{
    Started,
    Finished,
    Doing,
    ProgressUpdated
}

/// <summary>
/// コマンドログの追加情報ペイロード基底型です。
/// </summary>
public abstract record CommandLogPayload;

public sealed record ProcessOutputLogPayload(
    bool IsError,
    string Text,
    DateTimeOffset Timestamp) : CommandLogPayload;

/// <summary>
/// コマンドイベントバスで配信されるイベント本体です。
/// </summary>
public sealed record CommandBusEvent(
    CommandEventKind Kind,
    ICommand Command,
    string Detail = "",
    int Progress = 0,
    CommandLogPayload? Payload = null);

/// <summary>
/// コマンド実行イベントを配信・購読するバス契約です。
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
    void PublishDoing(ICommand command, string detail, CommandLogPayload payload);
    void PublishProgress(ICommand command, int progress);
    long DroppedEventCount { get; }
    int SubscriberCount { get; }
    IAsyncEnumerable<CommandBusEvent> ReadEventsAsync(CancellationToken cancellationToken = default);
}

public class CommandEventArgs(ICommand command) : EventArgs
{
    /// <summary>対象コマンドです。</summary>
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

    /// <summary>ログ本文です。</summary>
    public string Detail { get; }
    /// <summary>追加の構造化ペイロードです。</summary>
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
