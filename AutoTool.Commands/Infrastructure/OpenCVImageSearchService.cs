using System.Windows.Media;
using AutoTool.Commands.Services;
using Color = System.Windows.Media.Color;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// OpenCVを使用した画像検索サービスの実装
/// </summary>
public class OpenCVImageSearchService : IImageSearchService
{
    public async Task<OpenCvSharp.Point?> SearchImageAsync(
        string imagePath,
        CancellationToken cancellationToken,
        double threshold = 0.9,
        Color? searchColor = null,
        string? windowTitle = null,
        string? windowClassName = null)
    {
        return await OpenCvImageSearchHelper.SearchImageAsync(
            imagePath,
            cancellationToken,
            threshold,
            searchColor,
            windowTitle ?? string.Empty,
            windowClassName ?? string.Empty);
    }
}


