using AutoTool.Commands.Services;
using System.Windows.Media;

namespace AutoTool.Commands.Commands;

public sealed class FindImageOptions
{
    public string ImagePath { get; init; } = string.Empty;
    public double Threshold { get; init; } = 0.8;
    public Color? SearchColor { get; init; }
    public int Timeout { get; init; }
    public int Interval { get; init; } = 500;
    public string WindowTitle { get; init; } = string.Empty;
    public string WindowClassName { get; init; } = string.Empty;
}

public readonly record struct FindImageResult(bool Found, MatchPoint? Point, int ElapsedMilliseconds);

public static class FindImageExecutor
{
    public static async Task<FindImageResult> ExecuteAsync(
        FindImageOptions options,
        Func<string, double, Color?, string?, string?, CancellationToken, Task<MatchPoint?>> searchAsync,
        Action<int>? reportProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(searchAsync);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timeout = Math.Max(0, options.Timeout);
        var interval = Math.Max(0, options.Interval);

        // timeout=0 は 1 回だけ即時検索として扱う
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
                cancellationToken);

            if (point != null)
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

            await Task.Delay(interval, cancellationToken);
        }

        return new FindImageResult(false, null, (int)stopwatch.ElapsedMilliseconds);
    }
}
