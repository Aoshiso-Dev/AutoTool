using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// 座標クリックコマンド
/// </summary>
public class ClickCommand : BaseCommand, IClickCommand
{
    private readonly IMouseService _mouseService;

    public new IClickCommandSettings Settings => (IClickCommandSettings)base.Settings;

    public ClickCommand(ICommand? parent, ICommandSettings settings, IMouseService mouseService)
        : base(parent, settings)
    {
        _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        await _mouseService.ClickAsync(
            Settings.X,
            Settings.Y,
            Settings.Button,
            Settings.WindowTitle,
            Settings.WindowClassName);

        var targetDescription = string.IsNullOrEmpty(Settings.WindowTitle) && string.IsNullOrEmpty(Settings.WindowClassName)
            ? "グローバル"
            : $"{Settings.WindowTitle}[{Settings.WindowClassName}]";

        RaiseDoingCommand($"クリックしました。対象: {targetDescription} ({Settings.X}, {Settings.Y})");
        return true;
    }
}
