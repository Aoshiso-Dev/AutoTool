using CommunityToolkit.Mvvm.Messaging;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Message;

namespace MacroPanels.Command.Commands;

/// <summary>
/// コマンドの基底クラス
/// </summary>
public abstract class BaseCommand : ICommand
{
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
        OnStartCommand += (_, _) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
        OnDoingCommand += (_, log) => WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, log ?? string.Empty));
        OnFinishCommand += (_, _) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
    }

    public virtual async Task<bool> Execute(CancellationToken cancellationToken)
    {
        OnStartCommand?.Invoke(this, EventArgs.Empty);
        var result = await DoExecuteAsync(cancellationToken);
        OnFinishCommand?.Invoke(this, EventArgs.Empty);
        return result;
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
        WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(this, progress));
    }

    /// <summary>
    /// 子コマンドの進捗をリセットします
    /// </summary>
    protected void ResetChildrenProgress()
    {
        foreach (var command in Children)
        {
            WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
        }
    }

    /// <summary>
    /// 子コマンドを順に実行します
    /// </summary>
    protected async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("子要素がありません。");
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
}
