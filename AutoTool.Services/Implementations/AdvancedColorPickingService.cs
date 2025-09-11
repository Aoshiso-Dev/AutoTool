using System.Drawing;
using AutoTool.Services.Abstractions;
using AutoTool.Services.ColorPicking;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Implementations;

/// <summary>
/// 高度な色選択機能サービス
/// </summary>
public class AdvancedColorPickingService : IAdvancedColorPickingService
{
    private readonly ILogger<AdvancedColorPickingService> _logger;
    private readonly IColorPickService _colorPickService;
    private readonly List<ColorInfo> _colorHistory = new();

    public AdvancedColorPickingService(
        ILogger<AdvancedColorPickingService> logger,
        IColorPickService colorPickService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _colorPickService = colorPickService ?? throw new ArgumentNullException(nameof(colorPickService));
    }

    public IReadOnlyList<ColorInfo> ColorHistory => _colorHistory.AsReadOnly();
    public ColorInfo? LastColorInfo => _colorHistory.LastOrDefault();

    public async Task<ColorInfo?> GetColorInfoAtPositionAsync(int x, int y)
    {
        await Task.Yield();
        
        var color = _colorPickService.GetColorAt(x, y);
        var colorInfo = new ColorInfo
        {
            Color = color,
            Position = new Point(x, y),
            CapturedAt = DateTime.Now
        };

        AddToHistory(colorInfo);
        return colorInfo;
    }

    public async Task<ColorInfo?> GetColorInfoAtCurrentMousePositionAsync()
    {
        await Task.Yield();
        
        var color = _colorPickService.GetColorAtCurrentMousePosition();
        // マウス位置を取得する必要がある場合は、IMouseServiceを注入
        var colorInfo = new ColorInfo
        {
            Color = color,
            Position = new Point(0, 0), // 実際のマウス位置を設定
            CapturedAt = DateTime.Now
        };

        AddToHistory(colorInfo);
        return colorInfo;
    }

    public async Task<ColorInfo?> GetAverageColorInRegionAsync(System.Windows.Rect region)
    {
        await Task.Yield();
        
        // 領域内の平均色を計算
        var colors = new List<Color>();
        var rect = new Rectangle((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height);
        
        for (int x = rect.X; x < rect.X + rect.Width; x += 5) // サンプリング間隔を5に設定
        {
            for (int y = rect.Y; y < rect.Y + rect.Height; y += 5)
            {
                colors.Add(_colorPickService.GetColorAt(x, y));
            }
        }

        if (colors.Count == 0) return null;

        var avgR = (int)colors.Average(c => c.R);
        var avgG = (int)colors.Average(c => c.G);
        var avgB = (int)colors.Average(c => c.B);
        
        var averageColor = Color.FromArgb(avgR, avgG, avgB);
        var colorInfo = new ColorInfo
        {
            Color = averageColor,
            Position = new Point((int)region.X, (int)region.Y),
            CapturedAt = DateTime.Now,
            Notes = $"Average color in region {region}"
        };

        AddToHistory(colorInfo);
        return colorInfo;
    }

    public async Task<ColorInfo?> FindSimilarColorAsync(Color targetColor, double tolerance = 10.0)
    {
        await Task.Yield();
        
        // 画面上で類似色を検索（簡易実装）
        // 実際の実装では、より効率的な検索アルゴリズムを使用
        
        return null; // TODO: 実装
    }

    public async Task<ColorHistogram?> GetColorHistogramAsync(System.Windows.Rect region)
    {
        await Task.Yield();
        
        var colorCounts = new Dictionary<Color, int>();
        var rect = new Rectangle((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height);
        int totalPixels = 0;
        
        for (int x = rect.X; x < rect.X + rect.Width; x += 2)
        {
            for (int y = rect.Y; y < rect.Y + rect.Height; y += 2)
            {
                var color = _colorPickService.GetColorAt(x, y);
                colorCounts[color] = colorCounts.GetValueOrDefault(color, 0) + 1;
                totalPixels++;
            }
        }

        if (colorCounts.Count == 0) return null;

        var dominantColor = colorCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        
        return new ColorHistogram
        {
            Colors = colorCounts,
            TotalPixels = totalPixels,
            DominantColor = dominantColor
        };
    }

    public ColorHistoryStatistics GetColorHistoryStatistics()
    {
        var stats = new ColorHistoryStatistics
        {
            TotalColors = _colorHistory.Count,
            FirstColorCapturedAt = _colorHistory.FirstOrDefault()?.CapturedAt,
            LastColorCapturedAt = _colorHistory.LastOrDefault()?.CapturedAt
        };

        if (_colorHistory.Count > 0)
        {
            var colorGroups = _colorHistory.GroupBy(c => c.Color);
            var mostUsed = colorGroups.OrderByDescending(g => g.Count()).First();
            stats.MostUsedColor = mostUsed.Key;
        }

        return stats;
    }

    public void ClearColorHistory()
    {
        _colorHistory.Clear();
        _logger.LogDebug("Color history cleared");
    }

    public IEnumerable<Color> GenerateColorPalette(int maxColors = 16)
    {
        if (_colorHistory.Count == 0) return Enumerable.Empty<Color>();

        return _colorHistory
            .GroupBy(c => c.Color)
            .OrderByDescending(g => g.Count())
            .Take(maxColors)
            .Select(g => g.Key);
    }

    public IEnumerable<Color> GenerateComplementaryPalette(Color baseColor)
    {
        var colors = new List<Color> { baseColor };
        
        // 補色を計算
        var complementaryColor = Color.FromArgb(255 - baseColor.R, 255 - baseColor.G, 255 - baseColor.B);
        colors.Add(complementaryColor);
        
        // 類似色を生成
        colors.Add(Color.FromArgb(Math.Min(255, baseColor.R + 30), baseColor.G, baseColor.B));
        colors.Add(Color.FromArgb(baseColor.R, Math.Min(255, baseColor.G + 30), baseColor.B));
        colors.Add(Color.FromArgb(baseColor.R, baseColor.G, Math.Min(255, baseColor.B + 30)));
        
        return colors;
    }

    private void AddToHistory(ColorInfo colorInfo)
    {
        _colorHistory.Add(colorInfo);
        
        // 履歴の最大数を制限（例：1000件）
        if (_colorHistory.Count > 1000)
        {
            _colorHistory.RemoveAt(0);
        }
        
        _logger.LogTrace("Added color to history: {Color} at {Position}", colorInfo.Color, colorInfo.Position);
    }
}