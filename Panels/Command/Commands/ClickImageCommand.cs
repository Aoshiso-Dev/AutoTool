using System.Diagnostics;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// 画像クリックコマンド
/// </summary>
public class ClickImageCommand : BaseCommand, IClickImageCommand
{
    private readonly IImageSearchService _imageSearchService;
    private readonly IMouseService _mouseService;

    public new IClickImageCommandSettings Settings => (IClickImageCommandSettings)base.Settings;

    public ClickImageCommand(
        ICommand? parent,
        ICommandSettings settings,
        IImageSearchService imageSearchService,
        IMouseService mouseService)
        : base(parent, settings)
    {
        _imageSearchService = imageSearchService ?? throw new ArgumentNullException(nameof(imageSearchService));
        _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
        {
            var point = await _imageSearchService.SearchImageAsync(
                Settings.ImagePath,
                cancellationToken,
                Settings.Threshold,
                Settings.SearchColor,
                Settings.WindowTitle,
                Settings.WindowClassName);

            if (point != null)
            {
                await _mouseService.ClickAsync(
                    point.Value.X,
                    point.Value.Y,
                    Settings.Button,
                    Settings.WindowTitle,
                    Settings.WindowClassName);

                RaiseDoingCommand($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                return true;
            }

            if (cancellationToken.IsCancellationRequested) return false;

            ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

            await Task.Delay(Settings.Interval, cancellationToken);
        }

        RaiseDoingCommand("画像が見つかりませんでした。");
        return false;
    }
}
    