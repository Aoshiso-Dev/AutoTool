using AutoTool.Commands.Interface;

namespace AutoTool.Commands.Commands;

/// <summary>
/// ループコマンド
/// </summary>
public class LoopCommand : BaseCommand, ILoopCommand
{
    public new ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

    public LoopCommand() { }

    public LoopCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override async ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
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

                if (!await command.Execute(cancellationToken).ConfigureAwait(false))
                {
                    // LoopBreak は正常なループ離脱として扱い、その他の false は失敗として伝播
                    return command is ILoopBreakCommand;
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

    protected override ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        ResetChildrenProgress();
        return ValueTask.FromResult(true);
    }
}

/// <summary>
/// ループ中断コマンド
/// </summary>
public class LoopBreakCommand : BaseCommand, ILoopBreakCommand
{
    public LoopBreakCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken) => ValueTask.FromResult(false);
}

/// <summary>
/// リトライコマンド
/// </summary>
public class RetryCommand : BaseCommand, IRetryCommand
{
    public new IRetryCommandSettings Settings => (IRetryCommandSettings)base.Settings;

    public RetryCommand() { }

    public RetryCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override async ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
        {
            throw new InvalidOperationException("リトライブロック内に要素がありません。");
        }

        for (var attempt = 1; attempt <= Settings.RetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RaiseDoingCommand($"リトライ実行 {attempt}/{Settings.RetryCount} 回目");

            ResetChildrenProgress();
            var succeeded = true;

            foreach (var command in Children)
            {
                if (!await command.Execute(cancellationToken).ConfigureAwait(false))
                {
                    succeeded = false;
                    break;
                }
            }

            if (succeeded)
            {
                RaiseDoingCommand($"リトライ成功 ({attempt}/{Settings.RetryCount})");
                return true;
            }

            if (attempt >= Settings.RetryCount)
            {
                break;
            }

            if (Settings.RetryInterval > 0)
            {
                RaiseDoingCommand($"次回リトライまで {Settings.RetryInterval}ms 待機します。");
                await Task.Delay(Settings.RetryInterval, cancellationToken).ConfigureAwait(false);
            }
        }

        RaiseDoingCommand($"リトライ失敗: {Settings.RetryCount} 回実行しても成功しませんでした。");
        return false;
    }
}

/// <summary>
/// リトライ終了コマンド
/// </summary>
public class RetryEndCommand : BaseCommand
{
    public RetryEndCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        ResetChildrenProgress();
        return ValueTask.FromResult(true);
    }
}
