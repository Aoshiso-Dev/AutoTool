using System.Diagnostics;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// 画像待機コマンド
/// </summary>
public class WaitImageCommand : BaseCommand, IWaitImageCommand
{
    private readonly IImageSearchService _imageSearchService;

    public new IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;

    public WaitImageCommand(ICommand? parent, ICommandSettings settings, IImageSearchService imageSearchService)
        : base(parent, settings)
    {
        _imageSearchService = imageSearchService ?? throw new ArgumentNullException(nameof(imageSearchService));
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
