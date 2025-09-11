using System.Drawing;

namespace AutoTool.Services.ColorPicking;

/// <summary>
/// 色情報
/// </summary>
public class ColorInfo
{
    public Color Color { get; set; }
    public Point Position { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
}

/// <summary>
/// ウィンドウキャプチャ結果
/// </summary>
public class WindowCaptureResult
{
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
    public Point Position { get; set; }
    public Size Size { get; set; }
    public IntPtr Handle { get; set; }
}

/// <summary>
/// キーキャプチャ結果
/// </summary>
public class KeyCaptureResult
{
    public string Key { get; set; } = string.Empty;
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 色ヒストグラム
/// </summary>
public class ColorHistogram
{
    public Dictionary<Color, int> Colors { get; set; } = new();
    public int TotalPixels { get; set; }
    public Color DominantColor { get; set; }
}

/// <summary>
/// 色履歴統計
/// </summary>
public class ColorHistoryStatistics
{
    public int TotalColors { get; set; }
    public Color? MostUsedColor { get; set; }
    public DateTime? FirstColorCapturedAt { get; set; }
    public DateTime? LastColorCapturedAt { get; set; }
}