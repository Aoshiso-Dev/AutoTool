using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AutoTool.Commands.Services;
using AutoTool.Commands.Threading;
using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// ICommandEventBus の標準実装
/// </summary>
public sealed class CommandEventBus : ICommandEventBus
{
    private readonly object _gate = new();
    private readonly CommandEventBusOptions _options;
    private readonly Dictionary<int, Channel<CommandBusEvent>> _subscribers = [];
    private int _nextSubscriberId;
    private long _droppedEventCount;

    public CommandEventBus(CommandEventBusOptions? options = null)
    {
        _options = options ?? new();
        _options.SubscriberBufferSize = Math.Max(1, _options.SubscriberBufferSize);
        _options.DropWarningInitialThreshold = Math.Max(1, _options.DropWarningInitialThreshold);
        _options.DropWarningInterval = Math.Max(1, _options.DropWarningInterval);
    }

    public event EventHandler<CommandEventArgs>? Started;
    public event EventHandler<CommandEventArgs>? Finished;
    public event EventHandler<CommandLogEventArgs>? Doing;
    public event EventHandler<CommandProgressEventArgs>? ProgressUpdated;
    public long DroppedEventCount => Interlocked.Read(ref _droppedEventCount);
    public int SubscriberCount
    {
        get
        {
            lock (_gate)
            {
                return _subscribers.Count;
            }
        }
    }

    public void PublishStarted(ICommand command)
    {
        PublishToSubscribers(new CommandBusEvent(CommandEventKind.Started, command));
        Started?.Invoke(this, new CommandEventArgs(command));
    }

    public void PublishFinished(ICommand command)
    {
        PublishToSubscribers(new CommandBusEvent(CommandEventKind.Finished, command));
        Finished?.Invoke(this, new CommandEventArgs(command));
    }

    public void PublishDoing(ICommand command, string detail)
    {
        PublishToSubscribers(new CommandBusEvent(CommandEventKind.Doing, command, detail));
        Doing?.Invoke(this, new CommandLogEventArgs(command, detail));
    }

    public void PublishDoing(ICommand command, string detail, CommandLogPayload payload)
    {
        PublishToSubscribers(new CommandBusEvent(CommandEventKind.Doing, command, detail, Payload: payload));
        Doing?.Invoke(this, new CommandLogEventArgs(command, detail, payload));
    }

    public void PublishProgress(ICommand command, int progress)
    {
        PublishToSubscribers(new CommandBusEvent(CommandEventKind.ProgressUpdated, command, "", progress));
        ProgressUpdated?.Invoke(this, new CommandProgressEventArgs(command, progress));
    }

    public IAsyncEnumerable<CommandBusEvent> ReadEventsAsync(CancellationToken cancellationToken = default)
    {
        return ReadEventsInternalAsync(cancellationToken);
    }

    private async IAsyncEnumerable<CommandBusEvent> ReadEventsInternalAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriberChannel = Channel.CreateBounded<CommandBusEvent>(
            new BoundedChannelOptions(_options.SubscriberBufferSize)
            {
                SingleReader = true,
                SingleWriter = false,
                // 古いイベントを優先的に破棄し、最新の進捗・状態を UI に反映しやすくする
                FullMode = BoundedChannelFullMode.DropOldest
            });

        var subscriberId = AddSubscriber(subscriberChannel);
        try
        {
            await foreach (var ev in subscriberChannel.Reader.ReadAllAsync().ConfigureAwaitFalse(cancellationToken))
            {
                yield return ev;
            }
        }
        finally
        {
            RemoveSubscriber(subscriberId, subscriberChannel);
        }
    }

    private int AddSubscriber(Channel<CommandBusEvent> subscriberChannel)
    {
        lock (_gate)
        {
            var subscriberId = ++_nextSubscriberId;
            _subscribers[subscriberId] = subscriberChannel;
            return subscriberId;
        }
    }

    private void RemoveSubscriber(int subscriberId, Channel<CommandBusEvent> subscriberChannel)
    {
        lock (_gate)
        {
            _subscribers.Remove(subscriberId);
        }

        subscriberChannel.Writer.TryComplete();
    }

    private void PublishToSubscribers(CommandBusEvent ev)
    {
        Channel<CommandBusEvent>[] channels;
        lock (_gate)
        {
            if (_subscribers.Count == 0)
            {
                return;
            }

            channels = _subscribers.Values.ToArray();
        }

        foreach (var channel in channels)
        {
            if (!channel.Writer.TryWrite(ev))
            {
                var dropped = Interlocked.Increment(ref _droppedEventCount);
                if (dropped >= _options.DropWarningInitialThreshold
                    && (dropped == _options.DropWarningInitialThreshold
                        || dropped % _options.DropWarningInterval == 0))
                {
                    System.Diagnostics.Trace.TraceWarning(
                        $"CommandEventBus でイベントを破棄しました。dropped={dropped}, subscribers={SubscriberCount}");
                }
            }
        }
    }
}
