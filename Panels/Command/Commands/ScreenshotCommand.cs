using System.IO;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// スクリーンショットコマンド
/// </summary>
public class ScreenshotCommand : BaseCommand, IScreenshotCommand
{
    private readonly IScreenCaptureService _screenCaptureService;

    public new IScreenshotCommandSettings Settings => (IScreenshotCommandSettings)base.Settings;

    public ScreenshotCommand(ICommand? parent, ICommandSettings settings, IScreenCaptureService screenCaptureService)
        : base(parent, settings)
    {
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var dir = string.IsNullOrWhiteSpace(Settings.SaveDirectory)
                ? Path.Combine(Environment.CurrentDirectory, "Screenshots")
                : Settings.SaveDirectory;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(dir, fileName);

            await _screenCaptureService.CaptureToFileAsync(
                filePath,
                Settings.WindowTitle,
                Settings.WindowClassName,
                cancellationToken);

            RaiseDoingCommand($"スクリーンショットを保存しました: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            RaiseDoingCommand($"スクリーンショットの保存に失敗しました: {ex.Message}");
            return false;
        }
    }
}
