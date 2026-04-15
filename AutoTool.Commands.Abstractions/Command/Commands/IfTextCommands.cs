using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Commands;

public class IfTextExistCommand : BaseCommand, IIfCommand, IIfTextExistCommand
{
    private readonly IOcrEngine _ocrEngine;

    public new IIfTextCommandSettings Settings => (IIfTextCommandSettings)base.Settings;

    public IfTextExistCommand(ICommand? parent, ICommandSettings settings, IOcrEngine ocrEngine)
        : base(parent, settings)
    {
        _ocrEngine = ocrEngine ?? throw new ArgumentNullException(nameof(ocrEngine));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("If内に要素がありません。");
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
            TessdataPath = Settings.TessdataPath
        }, cancellationToken);

        var matched = TextMatchEvaluator.IsMatched(result.Text, result.Confidence, Settings);
        if (matched)
        {
            RaiseDoingCommand($"文字が見つかりました。text=\"{result.Text}\" confidence={result.Confidence:F1}");
            return await ExecuteChildrenAsync(cancellationToken);
        }

        RaiseDoingCommand("文字が見つかりませんでした。");
        return true;
    }
}

public class IfTextNotExistCommand : BaseCommand, IIfCommand, IIfTextNotExistCommand
{
    private readonly IOcrEngine _ocrEngine;

    public new IIfTextCommandSettings Settings => (IIfTextCommandSettings)base.Settings;

    public IfTextNotExistCommand(ICommand? parent, ICommandSettings settings, IOcrEngine ocrEngine)
        : base(parent, settings)
    {
        _ocrEngine = ocrEngine ?? throw new ArgumentNullException(nameof(ocrEngine));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("If内に要素がありません。");
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
            TessdataPath = Settings.TessdataPath
        }, cancellationToken);

        var matched = TextMatchEvaluator.IsMatched(result.Text, result.Confidence, Settings);
        if (!matched)
        {
            RaiseDoingCommand("文字が見つかりませんでした。");
            return await ExecuteChildrenAsync(cancellationToken);
        }

        RaiseDoingCommand($"文字が見つかりました。text=\"{result.Text}\" confidence={result.Confidence:F1}");
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
