using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// AI画像クリックコマンド
/// </summary>
public class ClickImageAICommand : BaseCommand, IClickImageAICommand
{
    private readonly IAIDetectionService _aiDetectionService;
    private readonly IMouseService _mouseService;

    public new IClickImageAICommandSettings Settings => (IClickImageAICommandSettings)base.Settings;

    public ClickImageAICommand(
        ICommand? parent,
        ICommandSettings settings,
        IAIDetectionService aiDetectionService,
        IMouseService mouseService)
        : base(parent, settings)
    {
        _aiDetectionService = aiDetectionService ?? throw new ArgumentNullException(nameof(aiDetectionService));
        _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        _aiDetectionService.Initialize(Settings.ModelPath, 640, true);

        var detections = _aiDetectionService.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        var targetDetections = detections.Where(d => d.ClassId == Settings.ClassID).ToList();

        if (targetDetections.Count > 0)
        {
            var best = targetDetections.OrderByDescending(d => d.Score).First();

            int centerX = best.Rect.X + best.Rect.Width / 2;
            int centerY = best.Rect.Y + best.Rect.Height / 2;

            await _mouseService.ClickAsync(
                centerX,
                centerY,
                Settings.Button,
                Settings.WindowTitle,
                Settings.WindowClassName);

            RaiseDoingCommand($"AI画像クリックが完了しました。({centerX}, {centerY}) ClassId: {best.ClassId}, Score: {best.Score:F2}");
            return true;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        RaiseDoingCommand($"クラスID {Settings.ClassID} の画像が見つかりませんでした。");
        return false;
    }
}
