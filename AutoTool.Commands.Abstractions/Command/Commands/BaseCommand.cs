using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Commands;

/// <summary>
/// 郢ｧ・ｳ郢晄ｧｭﾎｦ郢晏ｳｨ繝ｻ陜難ｽｺ陟手ｼ斐￠郢晢ｽｩ郢ｧ・ｹ
/// </summary>
public abstract class BaseCommand : ICommand
{
    private ICommandEventBus _commandEventBus = NullCommandEventBus.Instance;

    public int LineNumber { get; set; }
    public bool IsEnabled { get; set; }
    public ICommand? Parent { get; set; }
    public IEnumerable<ICommand> Children { get; set; }
    public int NestLevel { get; set; }
    public ICommandSettings Settings { get; }
    public EventHandler? OnStartCommand { get; set; }
    public EventHandler? OnFinishCommand { get; set; }
    public EventHandler<string>? OnDoingCommand { get; set; }

    protected BaseCommand()
    {
        Children = [];
        Settings = new CommandSettings();
        InitializeEventHandlers();
    }

    protected BaseCommand(ICommand? parent, ICommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Parent = parent;
        NestLevel = parent is null ? 0 : parent.NestLevel + 1;
        Settings = settings;
        Children = [];
        InitializeEventHandlers();
    }

    private void InitializeEventHandlers()
    {
        OnStartCommand += (_, _) => _commandEventBus.PublishStarted(this);
        OnDoingCommand += (_, log) => _commandEventBus.PublishDoing(this, log ?? string.Empty);
        OnFinishCommand += (_, _) => _commandEventBus.PublishFinished(this);
    }

    public void SetEventBus(ICommandEventBus commandEventBus)
    {
        _commandEventBus = commandEventBus ?? NullCommandEventBus.Instance;
    }

    protected ICommandEventBus CommandEventBus => _commandEventBus;

    public virtual async Task<bool> Execute(CancellationToken cancellationToken)
    {
        try
        {
            CommandSettingsValidator.Validate(Settings);
            OnStartCommand?.Invoke(this, EventArgs.Empty);
            var result = await DoExecuteAsync(cancellationToken).ConfigureAwait(false);
            OnFinishCommand?.Invoke(this, EventArgs.Empty);
            return result;
        }
        catch (CommandSettingsValidationException ex)
        {
            throw new CommandValidationException(
                LineNumber,
                GetType().Name.Replace("Command", string.Empty, StringComparison.Ordinal),
                ex.ErrorCode,
                ex.SettingPropertyName,
                ex.Message,
                ex);
        }
    }

    protected abstract ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken);

    public bool CanExecute() => true;

    /// <summary>
    /// 郢晢ｽｭ郢ｧ・ｰ郢晢ｽ｡郢昴・縺晉ｹ晢ｽｼ郢ｧ・ｸ郢ｧ蝣､蛹ｱ髯ｦ蠕鯉ｼ邵ｺ・ｾ邵ｺ繝ｻ
    /// </summary>
    protected void RaiseDoingCommand(string message)
    {
        OnDoingCommand?.Invoke(this, message);
    }

    /// <summary>
    /// 鬨ｾ・ｲ隰仙干・定撻・ｱ陷ｻ鄙ｫ・邵ｺ・ｾ邵ｺ繝ｻ
    /// </summary>
    protected void ReportProgress(double elapsedMilliseconds, double totalMilliseconds)
    {
        int progress;
        if (totalMilliseconds <= 0)
        {
            progress = 100;
        }
        else
        {
            progress = (int)Math.Round((elapsedMilliseconds / totalMilliseconds) * 100);
            progress = Math.Clamp(progress, 0, 100);
        }
        _commandEventBus.PublishProgress(this, progress);
    }

    /// <summary>
    /// 陝・・縺慕ｹ晄ｧｭﾎｦ郢晏ｳｨ繝ｻ鬨ｾ・ｲ隰仙干・堤ｹ晢ｽｪ郢ｧ・ｻ郢昴・繝ｨ邵ｺ蜉ｱ竏ｪ邵ｺ繝ｻ
    /// </summary>
    protected void ResetChildrenProgress()
    {
        foreach (var command in Children)
        {
            _commandEventBus.PublishProgress(command, 0);
        }
    }

    /// <summary>
    /// 陝・・縺慕ｹ晄ｧｭﾎｦ郢晏ｳｨ・帝ｬ・・竊楢楜貅ｯ・｡蠕鯉ｼ邵ｺ・ｾ邵ｺ繝ｻ
    /// </summary>
    protected async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
        {
            throw new InvalidOperationException("No child commands are configured. Add at least one command before executing children.");
        }

        ResetChildrenProgress();

        foreach (var command in Children)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (!await command.Execute(cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class NullCommandEventBus : ICommandEventBus
    {
        public static NullCommandEventBus Instance { get; } = new();

        public event EventHandler<CommandEventArgs>? Started { add { } remove { } }
        public event EventHandler<CommandEventArgs>? Finished { add { } remove { } }
        public event EventHandler<CommandLogEventArgs>? Doing { add { } remove { } }
        public event EventHandler<CommandProgressEventArgs>? ProgressUpdated { add { } remove { } }
        public long DroppedEventCount => 0;
        public int SubscriberCount => 0;

        public void PublishStarted(ICommand command) { }
        public void PublishFinished(ICommand command) { }
        public void PublishDoing(ICommand command, string detail) { }
        public void PublishDoing(ICommand command, string detail, CommandLogPayload payload) { }
        public void PublishProgress(ICommand command, int progress) { }
        public async IAsyncEnumerable<CommandBusEvent> ReadEventsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

