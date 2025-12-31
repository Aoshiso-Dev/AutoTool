using MacroPanels.Command.Interface;

namespace MacroPanels.Command.Commands;

/// <summary>
/// ループコマンド
/// </summary>
public class LoopCommand : BaseCommand, ILoopCommand
{
    public new ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

    public LoopCommand() { }

    public LoopCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("ループ内に要素がありません。");
        }

        RaiseDoingCommand("ループを開始します。");

        for (int i = 0; i < Settings.LoopCount; i++)
        {
            ResetChildrenProgress();

            foreach (var command in Children)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (!await command.Execute(cancellationToken))
                {
                    return true;
                }
            }

            ReportProgress(i + 1, Settings.LoopCount);
        }

        RaiseDoingCommand("ループを終了します。");
        return true;
    }
}

/// <summary>
/// ループ終了コマンド
/// </summary>
public class LoopEndCommand : BaseCommand, IEndLoopCommand
{
    public new ILoopEndCommandSettings Settings => (ILoopEndCommandSettings)base.Settings;

    public LoopEndCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        ResetChildrenProgress();
        return Task.FromResult(true);
    }
}

/// <summary>
/// ループ中断コマンド
/// </summary>
public class LoopBreakCommand : BaseCommand, ILoopBreakCommand
{
    public LoopBreakCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
