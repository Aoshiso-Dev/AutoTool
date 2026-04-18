using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Commands;

public class IfTextExistCommand : BaseCommand, IIfCommand, IIfTextExistCommand
{
    private readonly IOcrEngine _ocrEngine;
    private readonly IPathResolver _pathResolver;

    public new IIfTextCommandSettings Settings => (IIfTextCommandSettings)base.Settings;

    public IfTextExistCommand(ICommand? parent, ICommandSettings settings, IOcrEngine ocrEngine, IPathResolver pathResolver)
        : base(parent, settings)
    {
        ArgumentNullException.ThrowIfNull(ocrEngine);
        ArgumentNullException.ThrowIfNull(pathResolver);
        _ocrEngine = ocrEngine;
        _pathResolver = pathResolver;
    }

    protected override async ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
        {
            throw new InvalidOperationException("条件ブロック内に実行するコマンドがありません。");
        }

        var result = await _ocrEngine.ExtractTextAsync(new OcrRequest
        {
            X = Settings.X,
            Y = Settings.Y,
            Width = Settings.Width,
            Height = Settings.Height,
            WindowTitle = Settings.WindowTitle,
            WindowClassName = Settings.WindowClassName,
            Language = Settings.Language,
            PageSegmentationMode = Settings.PageSegmentationMode,
            Whitelist = Settings.Whitelist,
            PreprocessMode = Settings.PreprocessMode,
            TessdataPath = string.IsNullOrWhiteSpace(Settings.TessdataPath)
                ? Settings.TessdataPath
                : _pathResolver.ToAbsolutePath(Settings.TessdataPath)
        }, cancellationToken).ConfigureAwait(false);

        var matched = TextMatchEvaluator.IsMatched(result.Text, result.Confidence, Settings);
        if (matched)
        {
            RaiseDoingCommand($"文字が見つかりました。Text=\"{result.Text}\" confidence={result.Confidence:F1}");
            return await ExecuteChildrenAsync(cancellationToken).ConfigureAwait(false);
        }

        RaiseDoingCommand("文字が見つかりませんでした。");
        return true;
    }
}

public class IfTextNotExistCommand : BaseCommand, IIfCommand, IIfTextNotExistCommand
{
    private readonly IOcrEngine _ocrEngine;
    private readonly IPathResolver _pathResolver;

    public new IIfTextCommandSettings Settings => (IIfTextCommandSettings)base.Settings;

    public IfTextNotExistCommand(ICommand? parent, ICommandSettings settings, IOcrEngine ocrEngine, IPathResolver pathResolver)
        : base(parent, settings)
    {
        ArgumentNullException.ThrowIfNull(ocrEngine);
        ArgumentNullException.ThrowIfNull(pathResolver);
        _ocrEngine = ocrEngine;
        _pathResolver = pathResolver;
    }

    protected override async ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children is null || !Children.Any())
        {
            throw new InvalidOperationException("条件ブロック内に実行するコマンドがありません。");
        }

        var result = await _ocrEngine.ExtractTextAsync(new OcrRequest
        {
            X = Settings.X,
            Y = Settings.Y,
            Width = Settings.Width,
            Height = Settings.Height,
            WindowTitle = Settings.WindowTitle,
            WindowClassName = Settings.WindowClassName,
            Language = Settings.Language,
            PageSegmentationMode = Settings.PageSegmentationMode,
            Whitelist = Settings.Whitelist,
            PreprocessMode = Settings.PreprocessMode,
            TessdataPath = string.IsNullOrWhiteSpace(Settings.TessdataPath)
                ? Settings.TessdataPath
                : _pathResolver.ToAbsolutePath(Settings.TessdataPath)
        }, cancellationToken).ConfigureAwait(false);

        var matched = TextMatchEvaluator.IsMatched(result.Text, result.Confidence, Settings);
        if (!matched)
        {
            RaiseDoingCommand("文字が見つかりませんでした。");
            return await ExecuteChildrenAsync(cancellationToken).ConfigureAwait(false);
        }

        RaiseDoingCommand($"文字が見つかりました。Text=\"{result.Text}\" confidence={result.Confidence:F1}");
        return true;
    }
}

internal static class TextMatchEvaluator
{
    public static bool IsMatched(string extractedText, double confidence, IIfTextCommandSettings settings)
    {
        if (confidence < settings.MinConfidence)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.TargetText))
        {
            return !string.IsNullOrWhiteSpace(extractedText);
        }

        var comparison = settings.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return settings.MatchMode switch
        {
            "Equals" => string.Equals(extractedText.Trim(), settings.TargetText.Trim(), comparison),
            _ => extractedText.Contains(settings.TargetText, comparison)
        };
    }
}
