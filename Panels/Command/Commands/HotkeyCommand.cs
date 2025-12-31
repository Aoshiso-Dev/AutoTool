using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// ホットキーコマンド
/// </summary>
public class HotkeyCommand : BaseCommand, IHotkeyCommand
{
    private readonly IKeyboardService _keyboardService;

    public new IHotkeyCommandSettings Settings => (IHotkeyCommandSettings)base.Settings;

    public HotkeyCommand(ICommand? parent, ICommandSettings settings, IKeyboardService keyboardService)
        : base(parent, settings)
    {
        _keyboardService = keyboardService ?? throw new ArgumentNullException(nameof(keyboardService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        await _keyboardService.SendKeyAsync(
            Settings.Key,
            Settings.Ctrl,
            Settings.Alt,
            Settings.Shift,
            Settings.WindowTitle,
            Settings.WindowClassName);

        RaiseDoingCommand("ホットキーを送信しました。");
        return true;
    }
}
