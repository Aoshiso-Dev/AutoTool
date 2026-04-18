using AutoTool.Commands.Interface;

namespace AutoTool.Commands.Commands;

/// <summary>
/// ルートコマンド（マクロのエントリーポイント）
/// </summary>
public class RootCommand : BaseCommand, IRootCommand
{
    protected override ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken) => ValueTask.FromResult(true);
}

/// <summary>
/// 何もしないコマンド
/// </summary>
public class NothingCommand : BaseCommand, IRootCommand
{
    public NothingCommand() { }

    public NothingCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken) => ValueTask.FromResult(true);
}
