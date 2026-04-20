using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using System.IO;

namespace AutoTool.Commands.Commands;

/// <summary>
/// 設定検証で利用するエラーコード定数を集約し、判定結果を一貫した識別子で扱えるようにします。
/// </summary>
public static class CommandValidationErrorCodes
{
    public const string ImagePathRequired = "E_IMG_PATH_EMPTY";
    public const string ImagePathNotFound = "E_IMG_PATH_NOT_FOUND";
    public const string ThresholdOutOfRange = "E_THRESHOLD_RANGE";
    public const string TimeoutOutOfRange = "E_TIMEOUT_RANGE";
    public const string IntervalOutOfRange = "E_INTERVAL_RANGE";
    public const string HoldDurationOutOfRange = "E_HOLD_DURATION_RANGE";
    public const string ClickInjectionModeInvalid = "E_CLICK_INJECTION_MODE_INVALID";
    public const string WidthOutOfRange = "E_WIDTH_RANGE";
    public const string HeightOutOfRange = "E_HEIGHT_RANGE";
    public const string ConfidenceOutOfRange = "E_CONFIDENCE_RANGE";
    public const string MatchModeInvalid = "E_MATCH_MODE_INVALID";
    public const string VariableNameRequired = "E_VARIABLE_NAME_EMPTY";
    public const string VariableOperatorInvalid = "E_VARIABLE_OPERATOR_INVALID";
    public const string ProgramPathRequired = "E_PROGRAM_PATH_EMPTY";
    public const string ProgramPathNotFound = "E_PROGRAM_PATH_NOT_FOUND";
    public const string ModelPathRequired = "E_MODEL_PATH_EMPTY";
    public const string ModelPathNotFound = "E_MODEL_PATH_NOT_FOUND";
    public const string WaitOutOfRange = "E_WAIT_RANGE";
    public const string LoopCountOutOfRange = "E_LOOP_COUNT_RANGE";
    public const string RetryCountOutOfRange = "E_RETRY_COUNT_RANGE";
    public const string RetryIntervalOutOfRange = "E_RETRY_INTERVAL_RANGE";
    public const string TessdataPathNotFound = "E_TESSDATA_PATH_NOT_FOUND";
    public const string TessdataDataMissing = "E_TESSDATA_TRAINEDDATA_MISSING";
}

/// <summary>
/// 関連データを不変に保持するレコード型です。
/// </summary>
public sealed record CommandValidationIssue(string Code, string PropertyName, string Message);

/// <summary>
/// 不正な入力や実行時エラーの詳細を保持して呼び出し元へ通知します。
/// </summary>
public sealed class CommandSettingsValidationException : ArgumentException
{
    public string ErrorCode { get; }
    public string SettingPropertyName { get; }

    public CommandSettingsValidationException(CommandValidationIssue issue)
        : base($"[{issue.Code}] {issue.Message}", issue.PropertyName)
    {
        ErrorCode = issue.Code;
        SettingPropertyName = issue.PropertyName;
    }
}

/// <summary>
/// 入力値や設定値を検証し、エラー内容を収集します。
/// </summary>
public static class CommandSettingsValidator
{
    private static readonly HashSet<string> SupportedVariableOperators = new(StringComparer.Ordinal)
    {
        "==", "!=", ">", "<", ">=", "<="
    };

    private static readonly HashSet<string> SupportedTextMatchModes = new(StringComparer.Ordinal)
    {
        "Contains", "Equals"
    };

    private static readonly HashSet<string> SupportedClickInjectionModes = new(StringComparer.Ordinal)
    {
        "MouseEvent", "SendInput"
    };

    public static void Validate(
        ICommandSettings settings,
        IPathResolver? pathResolver = null,
        bool includeExistenceChecks = false)
    {
        var issues = GetIssues(settings, pathResolver, includeExistenceChecks);
        if (issues.Count > 0)
        {
            throw new CommandSettingsValidationException(issues[0]);
        }
    }

    public static IReadOnlyList<CommandValidationIssue> GetIssues(
        ICommandSettings settings,
        IPathResolver? pathResolver = null,
        bool includeExistenceChecks = false)
    {
        ArgumentNullException.ThrowIfNull(settings);
        List<CommandValidationIssue> issues = [];

        if (settings is IWaitImageCommandSettings waitImage)
        {
            ValidateImagePath(waitImage.ImagePath, nameof(waitImage.ImagePath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(waitImage.Threshold, nameof(waitImage.Threshold), issues);
            ValidateNonNegative(waitImage.Timeout, nameof(waitImage.Timeout), CommandValidationErrorCodes.TimeoutOutOfRange, "タイムアウトは0以上で指定してください。", issues);
            ValidateNonNegative(waitImage.Interval, nameof(waitImage.Interval), CommandValidationErrorCodes.IntervalOutOfRange, "検索間隔は0以上で指定してください。", issues);
        }

        if (settings is IFindImageCommandSettings findImage)
        {
            ValidateImagePath(findImage.ImagePath, nameof(findImage.ImagePath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(findImage.Threshold, nameof(findImage.Threshold), issues);
            ValidateNonNegative(findImage.Timeout, nameof(findImage.Timeout), CommandValidationErrorCodes.TimeoutOutOfRange, "タイムアウトは0以上で指定してください。", issues);
            ValidateNonNegative(findImage.Interval, nameof(findImage.Interval), CommandValidationErrorCodes.IntervalOutOfRange, "検索間隔は0以上で指定してください。", issues);
        }

        if (settings is IIfImageCommandSettings ifImage)
        {
            ValidateImagePath(ifImage.ImagePath, nameof(ifImage.ImagePath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(ifImage.Threshold, nameof(ifImage.Threshold), issues);
        }

        if (settings is IClickImageCommandSettings clickImage)
        {
            ValidateImagePath(clickImage.ImagePath, nameof(clickImage.ImagePath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(clickImage.Threshold, nameof(clickImage.Threshold), issues);
            ValidateNonNegative(clickImage.Timeout, nameof(clickImage.Timeout), CommandValidationErrorCodes.TimeoutOutOfRange, "タイムアウトは0以上で指定してください。", issues);
            ValidateNonNegative(clickImage.Interval, nameof(clickImage.Interval), CommandValidationErrorCodes.IntervalOutOfRange, "検索間隔は0以上で指定してください。", issues);
            ValidateNonNegative(clickImage.HoldDurationMs, nameof(clickImage.HoldDurationMs), CommandValidationErrorCodes.HoldDurationOutOfRange, "押下維持時間は0以上で指定してください。", issues);
            ValidateClickInjectionMode(clickImage.ClickInjectionMode, nameof(clickImage.ClickInjectionMode), issues);
        }

        if (settings is IClickCommandSettings click)
        {
            ValidateNonNegative(click.HoldDurationMs, nameof(click.HoldDurationMs), CommandValidationErrorCodes.HoldDurationOutOfRange, "押下維持時間は0以上で指定してください。", issues);
            ValidateClickInjectionMode(click.ClickInjectionMode, nameof(click.ClickInjectionMode), issues);
        }

        if (settings is IFindTextCommandSettings findText)
        {
            ValidateTextSettings(findText.Width, findText.Height, findText.MinConfidence, findText.MatchMode, issues);
            ValidateNonNegative(findText.Timeout, nameof(findText.Timeout), CommandValidationErrorCodes.TimeoutOutOfRange, "タイムアウトは0以上で指定してください。", issues);
            ValidateNonNegative(findText.Interval, nameof(findText.Interval), CommandValidationErrorCodes.IntervalOutOfRange, "検索間隔は0以上で指定してください。", issues);
            ValidateTessdataPath(findText.TessdataPath, nameof(findText.TessdataPath), pathResolver, includeExistenceChecks, issues);
        }

        if (settings is IIfTextCommandSettings ifText)
        {
            ValidateTextSettings(ifText.Width, ifText.Height, ifText.MinConfidence, ifText.MatchMode, issues);
            ValidateTessdataPath(ifText.TessdataPath, nameof(ifText.TessdataPath), pathResolver, includeExistenceChecks, issues);
        }

        if (settings is ISetVariableCommandSettings setVariable && string.IsNullOrWhiteSpace(setVariable.Name))
        {
            Add(issues, CommandValidationErrorCodes.VariableNameRequired, nameof(setVariable.Name), "変数名は必須です。");
        }

        if (settings is ISetVariableOCRCommandSettings setVariableOcr)
        {
            if (string.IsNullOrWhiteSpace(setVariableOcr.Name))
            {
                Add(issues, CommandValidationErrorCodes.VariableNameRequired, nameof(setVariableOcr.Name), "変数名は必須です。");
            }

            ValidateTextSettings(setVariableOcr.Width, setVariableOcr.Height, setVariableOcr.MinConfidence, "Contains", issues);
            ValidateTessdataPath(setVariableOcr.TessdataPath, nameof(setVariableOcr.TessdataPath), pathResolver, includeExistenceChecks, issues);
        }

        if (settings is IIfVariableCommandSettings ifVariable)
        {
            if (string.IsNullOrWhiteSpace(ifVariable.Name))
            {
                Add(issues, CommandValidationErrorCodes.VariableNameRequired, nameof(ifVariable.Name), "比較する変数名は必須です。");
            }

            if (!SupportedVariableOperators.Contains(ifVariable.Operator))
            {
                Add(issues, CommandValidationErrorCodes.VariableOperatorInvalid, nameof(ifVariable.Operator), $"未対応の演算子です: {ifVariable.Operator}");
            }
        }

        if (settings is IExecuteCommandSettings execute)
        {
            if (string.IsNullOrWhiteSpace(execute.ProgramPath))
            {
                Add(issues, CommandValidationErrorCodes.ProgramPathRequired, nameof(execute.ProgramPath), "実行ファイルのパスは必須です。");
            }
            else if (includeExistenceChecks && TryResolve(pathResolver, execute.ProgramPath, out var absoluteProgramPath) && !File.Exists(absoluteProgramPath))
            {
                Add(issues, CommandValidationErrorCodes.ProgramPathNotFound, nameof(execute.ProgramPath), $"実行ファイルが見つかりません: {execute.ProgramPath}");
            }
        }

        if (settings is ISetVariableAICommandSettings setVariableAi)
        {
            if (string.IsNullOrWhiteSpace(setVariableAi.Name))
            {
                Add(issues, CommandValidationErrorCodes.VariableNameRequired, nameof(setVariableAi.Name), "変数名は必須です。");
            }

            ValidateModelPath(setVariableAi.ModelPath, nameof(setVariableAi.ModelPath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(setVariableAi.ConfThreshold, nameof(setVariableAi.ConfThreshold), issues);
            ValidateProbability(setVariableAi.IoUThreshold, nameof(setVariableAi.IoUThreshold), issues);
        }

        if (settings is IClickImageAICommandSettings clickImageAi)
        {
            ValidateModelPath(clickImageAi.ModelPath, nameof(clickImageAi.ModelPath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(clickImageAi.ConfThreshold, nameof(clickImageAi.ConfThreshold), issues);
            ValidateProbability(clickImageAi.IoUThreshold, nameof(clickImageAi.IoUThreshold), issues);
            ValidateNonNegative(clickImageAi.HoldDurationMs, nameof(clickImageAi.HoldDurationMs), CommandValidationErrorCodes.HoldDurationOutOfRange, "押下維持時間は0以上で指定してください。", issues);
            ValidateClickInjectionMode(clickImageAi.ClickInjectionMode, nameof(clickImageAi.ClickInjectionMode), issues);
        }

        if (settings is IIfImageExistAISettings ifImageExistAi)
        {
            ValidateModelPath(ifImageExistAi.ModelPath, nameof(ifImageExistAi.ModelPath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(ifImageExistAi.ConfThreshold, nameof(ifImageExistAi.ConfThreshold), issues);
            ValidateProbability(ifImageExistAi.IoUThreshold, nameof(ifImageExistAi.IoUThreshold), issues);
        }

        if (settings is IIfImageNotExistAISettings ifImageNotExistAi)
        {
            ValidateModelPath(ifImageNotExistAi.ModelPath, nameof(ifImageNotExistAi.ModelPath), pathResolver, includeExistenceChecks, issues);
            ValidateProbability(ifImageNotExistAi.ConfThreshold, nameof(ifImageNotExistAi.ConfThreshold), issues);
            ValidateProbability(ifImageNotExistAi.IoUThreshold, nameof(ifImageNotExistAi.IoUThreshold), issues);
        }

        if (settings is IWaitCommandSettings wait)
        {
            ValidateNonNegative(wait.Wait, nameof(wait.Wait), CommandValidationErrorCodes.WaitOutOfRange, "待機時間は0以上で指定してください。", issues);
        }

        if (settings is ILoopCommandSettings loop && loop.LoopCount < 0)
        {
            Add(issues, CommandValidationErrorCodes.LoopCountOutOfRange, nameof(loop.LoopCount), "ループ回数は0以上で指定してください。");
        }

        if (settings is IRetryCommandSettings retry)
        {
            if (retry.RetryCount <= 0)
            {
                Add(issues, CommandValidationErrorCodes.RetryCountOutOfRange, nameof(retry.RetryCount), "リトライ回数は1以上で指定してください。");
            }

            ValidateNonNegative(
                retry.RetryInterval,
                nameof(retry.RetryInterval),
                CommandValidationErrorCodes.RetryIntervalOutOfRange,
                "リトライ間隔は0以上で指定してください。",
                issues);
        }

        return issues;
    }

    private static void ValidateImagePath(string imagePath, string propertyName, IPathResolver? pathResolver, bool includeExistenceChecks, ICollection<CommandValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Add(issues, CommandValidationErrorCodes.ImagePathRequired, propertyName, "画像パスは必須です。");
            return;
        }

        if (!includeExistenceChecks || !TryResolve(pathResolver, imagePath, out var absolutePath))
        {
            return;
        }

        if (!File.Exists(absolutePath))
        {
            Add(issues, CommandValidationErrorCodes.ImagePathNotFound, propertyName, $"検索画像が見つかりません: {imagePath}");
        }
    }

    private static void ValidateModelPath(string modelPath, string propertyName, IPathResolver? pathResolver, bool includeExistenceChecks, ICollection<CommandValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            Add(issues, CommandValidationErrorCodes.ModelPathRequired, propertyName, "モデルパスは必須です。");
            return;
        }

        if (!includeExistenceChecks || !TryResolve(pathResolver, modelPath, out var absolutePath))
        {
            return;
        }

        if (!File.Exists(absolutePath))
        {
            Add(issues, CommandValidationErrorCodes.ModelPathNotFound, propertyName, $"モデルファイルが見つかりません: {modelPath}");
        }
    }

    private static void ValidateTessdataPath(string tessdataPath, string propertyName, IPathResolver? pathResolver, bool includeExistenceChecks, ICollection<CommandValidationIssue> issues)
    {
        if (!includeExistenceChecks)
        {
            return;
        }

        var normalizedPath = string.IsNullOrWhiteSpace(tessdataPath) ? @".\Settings\tessdata" : tessdataPath;
        if (!TryResolve(pathResolver, normalizedPath, out var absolutePath))
        {
            return;
        }

        if (!Directory.Exists(absolutePath))
        {
            var message = string.IsNullOrWhiteSpace(tessdataPath)
                ? "tessdata ディレクトリ未指定のため ./Settings/tessdata を確認しましたが、フォルダが見つかりません。"
                : $"フォルダが見つかりません: {tessdataPath}";
            Add(issues, CommandValidationErrorCodes.TessdataPathNotFound, propertyName, message);
            return;
        }

        try
        {
            var hasTrainedData = Directory.EnumerateFiles(absolutePath, "*.traineddata", SearchOption.TopDirectoryOnly).Any();
            if (!hasTrainedData)
            {
                var message = string.IsNullOrWhiteSpace(tessdataPath)
                    ? "*.traineddata が見つかりません。./Settings/tessdata に OCR データを配置してください。"
                    : "*.traineddata が見つかりません。tessdata フォルダを選択してください。";
                Add(issues, CommandValidationErrorCodes.TessdataDataMissing, propertyName, message);
            }
        }
        catch
        {
            Add(issues, CommandValidationErrorCodes.TessdataPathNotFound, propertyName, "フォルダを確認できませんでした。アクセス権限を確認してください。");
        }
    }

    private static void ValidateProbability(double value, string propertyName, ICollection<CommandValidationIssue> issues)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
        {
            Add(issues, CommandValidationErrorCodes.ThresholdOutOfRange, propertyName, "値は0.0～1.0の範囲で指定してください。");
        }
    }

    private static void ValidateClickInjectionMode(string mode, string propertyName, ICollection<CommandValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(mode) || !SupportedClickInjectionModes.Contains(mode))
        {
            Add(issues, CommandValidationErrorCodes.ClickInjectionModeInvalid, propertyName, $"未対応のクリック注入方式です: {mode}");
        }
    }

    private static void ValidateNonNegative(int value, string propertyName, string errorCode, string message, ICollection<CommandValidationIssue> issues)
    {
        if (value < 0)
        {
            Add(issues, errorCode, propertyName, message);
        }
    }

    private static void ValidateTextSettings(int width, int height, double minConfidence, string matchMode, ICollection<CommandValidationIssue> issues)
    {
        if (width <= 0)
        {
            Add(issues, CommandValidationErrorCodes.WidthOutOfRange, "Width", "幅は1以上で指定してください。");
        }

        if (height <= 0)
        {
            Add(issues, CommandValidationErrorCodes.HeightOutOfRange, "Height", "高さは1以上で指定してください。");
        }

        if (double.IsNaN(minConfidence) || double.IsInfinity(minConfidence) || minConfidence < 0 || minConfidence > 100)
        {
            Add(issues, CommandValidationErrorCodes.ConfidenceOutOfRange, "MinConfidence", "最小信頼度は0～100の範囲で指定してください。");
        }

        if (!SupportedTextMatchModes.Contains(matchMode))
        {
            Add(issues, CommandValidationErrorCodes.MatchModeInvalid, "MatchMode", $"未対応のマッチ方式です: {matchMode}");
        }
    }

    private static bool TryResolve(IPathResolver? pathResolver, string path, out string absolutePath)
    {
        if (pathResolver is null)
        {
            absolutePath = path;
            return false;
        }

        absolutePath = pathResolver.ToAbsolutePath(path);
        return !string.IsNullOrWhiteSpace(absolutePath);
    }

    private static void Add(ICollection<CommandValidationIssue> issues, string code, string propertyName, string message)
    {
        if (issues.Any(i => string.Equals(i.Code, code, StringComparison.Ordinal) && string.Equals(i.PropertyName, propertyName, StringComparison.Ordinal)))
        {
            return;
        }

        issues.Add(new CommandValidationIssue(code, propertyName, message));
    }
}
