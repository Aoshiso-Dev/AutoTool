using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// プログラム実行コマンド
/// </summary>
public class ExecuteCommand : BaseCommand, IExecuteCommand
{
    private readonly IProcessService _processService;

    public new IExecuteCommandSettings Settings => (IExecuteCommandSettings)base.Settings;

    public ExecuteCommand(ICommand? parent, ICommandSettings settings, IProcessService processService)
        : base(parent, settings)
    {
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _processService.StartAsync(
                Settings.ProgramPath,
                Settings.Arguments,
                Settings.WorkingDirectory,
                Settings.WaitForExit,
                cancellationToken);

            RaiseDoingCommand($"プログラムを実行しました: {Settings.ProgramPath}");
            return true;
        }
        catch (Exception ex)
        {
            RaiseDoingCommand($"プログラム実行エラー: {ex.Message}");
            return false;
        }
    }
}
