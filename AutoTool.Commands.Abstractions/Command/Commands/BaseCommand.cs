using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Commands;

/// <summary>
/// コマンドの基底クラス
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
        Children = new List<ICommand>();
        Settings = new CommandSettings();
        InitializeEventHandlers();
    }

    protected BaseCommand(ICommand? parent, ICommandSettings settings)
    {
        Parent = parent;
        NestLevel = parent == null ? 0 : parent.NestLevel + 1;
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Children = new List<ICommand>();
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
            var result = await DoExecuteAsync(cancellationToken);
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

    protected abstract Task<bool> DoExecuteAsync(CancellationToken cancellationToken);

    public bool CanExecute() => true;

    /// <summary>
    /// ログメッセージを発行します
    /// </summary>
    protected void RaiseDoingCommand(string message)
    {
        OnDoingCommand?.Invoke(this, message);
    }

    /// <summary>
    /// 進捗を報告します
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
    /// 子コマンドの進捗をリセットします
    /// </summary>
    protected void ResetChildrenProgress()
    {
        foreach (var command in Children)
        {
            _commandEventBus.PublishProgress(command, 0);
        }
    }

    /// <summary>
    /// 子コマンドを順に実行します
    /// </summary>
    protected async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("条件ブロック内に実行するコマンドがありません。");
        }

        ResetChildrenProgress();

        foreach (var command in Children)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (!await command.Execute(cancellationToken))
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

        public void PublishStarted(ICommand command) { }
        public void PublishFinished(ICommand command) { }
        public void PublishDoing(ICommand command, string detail) { }
        public void PublishProgress(ICommand command, int progress) { }
    }
}
