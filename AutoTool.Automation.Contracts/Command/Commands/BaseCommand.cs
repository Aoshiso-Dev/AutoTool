using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Commands;

/// <summary>
/// コマンド実行の共通基底クラス
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
            try
            {
                return await DoExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                OnFinishCommand?.Invoke(this, EventArgs.Empty);
            }
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
    /// 実行中ログを発行する
    /// </summary>
    protected void RaiseDoingCommand(string message)
    {
        OnDoingCommand?.Invoke(this, message);
    }

    /// <summary>
    /// 経過時間から進捗率を通知する
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
    /// 子コマンドの進捗を 0 にリセットする
    /// </summary>
    protected void ResetChildrenProgress()
    {
        foreach (var command in Children)
        {
            _commandEventBus.PublishProgress(command, 0);
        }
    }

    /// <summary>
    /// 子コマンドを順次実行する
    /// </summary>
    protected async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
        {
            throw new InvalidOperationException("子コマンドが構成されていません。実行前に1つ以上のコマンドを追加してください。");
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

    /// <summary>
    /// イベント通知を行わない無効実装として動作し、イベントバスが不要な場面で安全に置き換えられるようにします。
    /// </summary>

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

