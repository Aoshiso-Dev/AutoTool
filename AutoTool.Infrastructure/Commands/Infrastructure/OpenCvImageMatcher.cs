using System.Linq;
using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Services;

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
        CommandColor? searchColor = null,
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

    public async Task<IReadOnlyList<MatchPoint>> SearchImagesAsync(
        string imagePath,
        CancellationToken cancellationToken,
        double threshold = 0.9,
        CommandColor? searchColor = null,
        string? windowTitle = null,
        string? windowClassName = null,
        int maxResults = 20)
    {
        var result = await OpenCvImageSearchHelper.SearchImagesAsync(
            imagePath,
            cancellationToken,
            threshold,
            searchColor,
            windowTitle ?? string.Empty,
            windowClassName ?? string.Empty,
            maxResults);

        return result.Select(p => new MatchPoint(p.X, p.Y)).ToArray();
    }
}



