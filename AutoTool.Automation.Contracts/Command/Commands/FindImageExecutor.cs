using AutoTool.Commands.Services;
using AutoTool.Commands.Model.Input;
using System.IO;

namespace AutoTool.Commands.Commands;

/// <summary>
/// オプション値を保持するレコード型です。
/// </summary>
public sealed record FindImageOptions
{
    public required string ImagePath { get; init; }
    public double Threshold { get; init; } = 0.8;
    public CommandColor? SearchColor { get; init; }
    public int Timeout { get; init; }
    public int Interval { get; init; } = 500;
    public string WindowTitle { get; init; } = string.Empty;
    public string WindowClassName { get; init; } = string.Empty;
}

/// <summary>
/// 実行結果や検出結果をまとめ、後続処理で必要な値を参照しやすくします。
/// </summary>

public readonly record struct FindImageResult(bool Found, MatchPoint? Point, int ElapsedMilliseconds);

/// <summary>
/// 画像探索処理を実行し、探索結果の座標や一致情報を呼び出し元へ返します。
/// </summary>
public static class FindImageExecutor
{
    public static async Task<FindImageResult> ExecuteAsync(
        FindImageOptions options,
        Func<string, double, CommandColor?, string?, string?, CancellationToken, Task<MatchPoint?>> searchAsync,
        Action<int>? reportProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(searchAsync);
        ValidateOptions(options);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timeout = Math.Max(0, options.Timeout);
        var interval = Math.Max(0, options.Interval);

        // timeout=0 は1回だけ検索を実行する
        var runOnce = timeout == 0;

        while (runOnce || stopwatch.ElapsedMilliseconds < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new FindImageResult(false, null, (int)stopwatch.ElapsedMilliseconds);
            }

            var point = await searchAsync(
                options.ImagePath,
                options.Threshold,
                options.SearchColor,
                options.WindowTitle,
                options.WindowClassName,
                cancellationToken).ConfigureAwait(false);

            if (point is not null)
            {
                reportProgress?.Invoke(100);
                return new FindImageResult(true, point, (int)stopwatch.ElapsedMilliseconds);
            }

            if (runOnce)
            {
                break;
            }

            var progress = timeout > 0 ? (int)((stopwatch.ElapsedMilliseconds * 100) / timeout) : 100;
            reportProgress?.Invoke(Math.Clamp(progress, 0, 100));

            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }

        return new FindImageResult(false, null, (int)stopwatch.ElapsedMilliseconds);
    }

    private static void ValidateOptions(FindImageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ImagePath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ImagePathRequired,
                    nameof(options.ImagePath),
                    "画像パスは必須です。"));
        }

        if (double.IsNaN(options.Threshold) || double.IsInfinity(options.Threshold) || options.Threshold < 0 || options.Threshold > 1)
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ThresholdOutOfRange,
                    nameof(options.Threshold),
                    "値は0.0～1.0の範囲で指定してください。"));
        }

        if (options.Timeout < 0)
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.TimeoutOutOfRange,
                    nameof(options.Timeout),
                    "タイムアウトは0以上で指定してください。"));
        }

        if (options.Interval < 0)
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.IntervalOutOfRange,
                    nameof(options.Interval),
                    "検索間隔は0以上で指定してください。"));
        }

        if (!File.Exists(options.ImagePath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ImagePathNotFound,
                    nameof(options.ImagePath),
                    $"検索画像が見つかりません: {options.ImagePath}"));
        }
    }
}
