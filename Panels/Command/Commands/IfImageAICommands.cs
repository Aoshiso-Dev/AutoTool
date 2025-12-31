using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// AI画像存在判定コマンド
/// </summary>
public class IfImageExistAICommand : BaseCommand, IIfCommand, IIfImageExistAICommand
{
    private readonly IAIDetectionService _aiDetectionService;

    public new IIfImageExistAISettings Settings => (IIfImageExistAISettings)base.Settings;

    public IfImageExistAICommand(ICommand? parent, ICommandSettings settings, IAIDetectionService aiDetectionService)
        : base(parent, settings)
    {
        _aiDetectionService = aiDetectionService ?? throw new ArgumentNullException(nameof(aiDetectionService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("If内に要素がありません。");
        }

        _aiDetectionService.Initialize(Settings.ModelPath, 640, true);

        var detections = _aiDetectionService.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        if (detections.Count > 0)
        {
            var best = detections.OrderByDescending(d => d.Score).First();

            if (best.ClassId == Settings.ClassID)
            {
                RaiseDoingCommand($"画像が見つかりました。({best.Rect.X}, {best.Rect.Y}) ClassId: {best.ClassId}");
                return await ExecuteChildrenAsync(cancellationToken);
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        RaiseDoingCommand("画像が見つかりませんでした。");
        return true;
    }
}

/// <summary>
/// AI画像非存在判定コマンド
/// </summary>
public class IfImageNotExistAICommand : BaseCommand, IIfCommand, IIfImageNotExistAICommand
{
    private readonly IAIDetectionService _aiDetectionService;

    public new IIfImageNotExistAISettings Settings => (IIfImageNotExistAISettings)base.Settings;

    public IfImageNotExistAICommand(ICommand? parent, ICommandSettings settings, IAIDetectionService aiDetectionService)
        : base(parent, settings)
    {
        _aiDetectionService = aiDetectionService ?? throw new ArgumentNullException(nameof(aiDetectionService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("If内に要素がありません。");
        }

        _aiDetectionService.Initialize(Settings.ModelPath, 640, true);

        var detections = _aiDetectionService.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        var targetDetections = detections.Where(d => d.ClassId == Settings.ClassID).ToList();

        if (targetDetections.Count == 0)
        {
            RaiseDoingCommand($"クラスID {Settings.ClassID} の画像が見つかりませんでした。");
            return await ExecuteChildrenAsync(cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        RaiseDoingCommand($"クラスID {Settings.ClassID} の画像が見つかりました。");
        return true;
    }
}
