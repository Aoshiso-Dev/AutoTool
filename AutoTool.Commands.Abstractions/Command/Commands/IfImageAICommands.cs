using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using System.IO;

namespace AutoTool.Commands.Commands;

/// <summary>
/// AI画像存在判定コマンド
/// </summary>
public class IfImageExistAICommand : BaseCommand, IIfCommand, IIfImageExistAICommand
{
    private readonly IObjectDetector _objectDetector;
    private readonly IPathResolver _pathResolver;

    public new IIfImageExistAISettings Settings => (IIfImageExistAISettings)base.Settings;

    public IfImageExistAICommand(ICommand? parent, ICommandSettings settings, IObjectDetector objectDetector, IPathResolver pathResolver)
        : base(parent, settings)
    {
        ArgumentNullException.ThrowIfNull(objectDetector);
        ArgumentNullException.ThrowIfNull(pathResolver);
        _objectDetector = objectDetector;
        _pathResolver = pathResolver;
    }

    protected override async ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
        {
            throw new InvalidOperationException("条件ブロック内に実行するコマンドがありません。");
        }

        var absoluteModelPath = _pathResolver.ToAbsolutePath(Settings.ModelPath);
        if (!File.Exists(absoluteModelPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ModelPathNotFound,
                    nameof(Settings.ModelPath),
                    $"モデルファイルが見つかりません: {Settings.ModelPath}"));
        }

        _objectDetector.Initialize(absoluteModelPath, 640, true);

        var detections = _objectDetector.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        if (detections.Count > 0)
        {
            var best = detections.OrderByDescending(d => d.Score).First();

            if (best.ClassId == Settings.ClassID)
            {
                RaiseDoingCommand($"画像が見つかりました。({best.Rect.X}, {best.Rect.Y}) ClassId: {best.ClassId}");
                return await ExecuteChildrenAsync(cancellationToken).ConfigureAwait(false);
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
    private readonly IObjectDetector _objectDetector;
    private readonly IPathResolver _pathResolver;

    public new IIfImageNotExistAISettings Settings => (IIfImageNotExistAISettings)base.Settings;

    public IfImageNotExistAICommand(ICommand? parent, ICommandSettings settings, IObjectDetector objectDetector, IPathResolver pathResolver)
        : base(parent, settings)
    {
        ArgumentNullException.ThrowIfNull(objectDetector);
        ArgumentNullException.ThrowIfNull(pathResolver);
        _objectDetector = objectDetector;
        _pathResolver = pathResolver;
    }

    protected override async ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
        {
            throw new InvalidOperationException("条件ブロック内に実行するコマンドがありません。");
        }

        var absoluteModelPath = _pathResolver.ToAbsolutePath(Settings.ModelPath);
        if (!File.Exists(absoluteModelPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ModelPathNotFound,
                    nameof(Settings.ModelPath),
                    $"モデルファイルが見つかりません: {Settings.ModelPath}"));
        }

        _objectDetector.Initialize(absoluteModelPath, 640, true);

        var detections = _objectDetector.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        var targetDetections = detections.Where(d => d.ClassId == Settings.ClassID).ToList();

        if (targetDetections.Count == 0)
        {
            RaiseDoingCommand($"クラスID {Settings.ClassID} の画像が見つかりませんでした。");
            return await ExecuteChildrenAsync(cancellationToken).ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        RaiseDoingCommand($"クラスID {Settings.ClassID} の画像が見つかりました。");
        return true;
    }
}





