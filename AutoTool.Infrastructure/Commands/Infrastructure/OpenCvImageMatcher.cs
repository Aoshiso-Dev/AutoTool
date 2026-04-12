using System.Windows.Media;
using AutoTool.Commands.Services;
using Color = System.Windows.Media.Color;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// OpenCVを使用した画像マッチャの実装
/// </summary>
public class OpenCvImageMatcher : IImageMatcher
{
    public async Task<MatchPoint?> SearchImageAsync(
        string imagePath,
        CancellationToken cancellationToken,
        double threshold = 0.9,
        Color? searchColor = null,
        string? windowTitle = null,
        string? windowClassName = null)
    {
        var result = await OpenCvImageSearchHelper.SearchImageAsync(
            imagePath,
            cancellationToken,
            threshold,
            searchColor,
            windowTitle ?? string.Empty,
            windowClassName ?? string.Empty);

        return result is { } p ? new MatchPoint(p.X, p.Y) : null;
    }
}


