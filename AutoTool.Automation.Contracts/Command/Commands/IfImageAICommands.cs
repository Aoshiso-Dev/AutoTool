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
        var absoluteLabelsPath = ResolveOptionalPath(Settings.LabelsPath);
        var targetClassId = ResolveTargetClassId(absoluteModelPath, absoluteLabelsPath, Settings.ClassID, Settings.LabelName);

        var detections = _objectDetector.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        var targetDetections = detections.Where(d => d.ClassId == targetClassId).ToList();
        if (targetDetections.Count > 0)
        {
            var best = targetDetections.OrderByDescending(d => d.Score).First();
            RaiseDoingCommand($"画像が見つかりました。({best.Rect.X}, {best.Rect.Y}) / クラスID: {best.ClassId}{BuildLabelSuffix()}");
            return await ExecuteChildrenAsync(cancellationToken).ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        RaiseDoingCommand($"対象が見つかりませんでした。クラスID: {targetClassId}{BuildLabelSuffix()}");
        return true;
    }

    private int ResolveTargetClassId(string absoluteModelPath, string? absoluteLabelsPath, int fallbackClassId, string? labelName)
    {
        if (string.IsNullOrWhiteSpace(labelName))
        {
            return fallbackClassId;
        }

        if (_objectDetector.TryResolveClassId(absoluteModelPath, labelName, absoluteLabelsPath, out var classId))
        {
            return classId;
        }

        throw new CommandSettingsValidationException(
            new CommandValidationIssue(
                CommandValidationErrorCodes.AiLabelNotFound,
                "LabelName",
                $"ラベル '{labelName}' をクラスIDへ解決できません。モデルのmetadataまたはラベルファイルを確認してください。"));
    }

    private string? ResolveOptionalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var absolutePath = _pathResolver.ToAbsolutePath(path);
        return string.IsNullOrWhiteSpace(absolutePath) ? null : absolutePath;
    }

    private string BuildLabelSuffix() => string.IsNullOrWhiteSpace(Settings.LabelName) ? string.Empty : $" / ラベル: {Settings.LabelName}";
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
        var absoluteLabelsPath = ResolveOptionalPath(Settings.LabelsPath);
        var targetClassId = ResolveTargetClassId(absoluteModelPath, absoluteLabelsPath, Settings.ClassID, Settings.LabelName);

        var detections = _objectDetector.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        var targetDetections = detections.Where(d => d.ClassId == targetClassId).ToList();

        if (targetDetections.Count == 0)
        {
            RaiseDoingCommand($"クラスID {targetClassId}{BuildLabelSuffix()} の画像が見つかりませんでした。");
            return await ExecuteChildrenAsync(cancellationToken).ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        RaiseDoingCommand($"クラスID {targetClassId}{BuildLabelSuffix()} の画像が見つかりました。");
        return true;
    }

    private int ResolveTargetClassId(string absoluteModelPath, string? absoluteLabelsPath, int fallbackClassId, string? labelName)
    {
        if (string.IsNullOrWhiteSpace(labelName))
        {
            return fallbackClassId;
        }

        if (_objectDetector.TryResolveClassId(absoluteModelPath, labelName, absoluteLabelsPath, out var classId))
        {
            return classId;
        }

        throw new CommandSettingsValidationException(
            new CommandValidationIssue(
                CommandValidationErrorCodes.AiLabelNotFound,
                "LabelName",
                $"ラベル '{labelName}' をクラスIDへ解決できません。モデルのmetadataまたはラベルファイルを確認してください。"));
    }

    private string? ResolveOptionalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var absolutePath = _pathResolver.ToAbsolutePath(path);
        return string.IsNullOrWhiteSpace(absolutePath) ? null : absolutePath;
    }

    private string BuildLabelSuffix() => string.IsNullOrWhiteSpace(Settings.LabelName) ? string.Empty : $" / ラベル: {Settings.LabelName}";
}





