using System.Drawing;
using AutoTool.Services.Abstractions;
using AutoTool.Services.ColorPicking;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Implementations;

/// <summary>
/// 色選択専用サービス
/// </summary>
public class ColorPickService : IColorPickService
{
    private readonly ILogger<ColorPickService> _logger;
    private readonly ICaptureService _captureService;
    private readonly IMouseService _mouseService;
    private readonly IKeyboardService _keyboardService;
    private bool _isActive;

    public ColorPickService(
        ILogger<ColorPickService> logger,
        ICaptureService captureService,
        IMouseService mouseService,
        IKeyboardService keyboardService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
        _keyboardService = keyboardService ?? throw new ArgumentNullException(nameof(keyboardService));
    }

    public bool IsColorPickerActive => _isActive;

    public async Task<Color?> CaptureColorFromScreenAsync()
    {
        return await StartColorPickerAsync();
    }

    public async Task<Color?> CaptureColorAtRightClickAsync()
    {
        return await WaitForRightClickColorAsync();
    }

    public Color GetColorAt(int x, int y)
    {
        return _captureService.GetColorAt(x, y);
    }

    public Color GetColorAt(Point position)
    {
        return _captureService.GetColorAt(position.X, position.Y);
    }

    public Color GetColorAtCurrentMousePosition()
    {
        var position = _mouseService.GetCurrentPosition();
        return GetColorAt(position.X, position.Y);
    }

    public string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public Color? HexToColor(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert hex to color: {Hex}", hex);
        }
        return null;
    }

    public System.Windows.Media.Color ToMediaColor(Color drawingColor)
    {
        return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
    }

    public Color ToDrawingColor(System.Windows.Media.Color mediaColor)
    {
        return Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
    }

    public void CancelColorPicker()
    {
        _isActive = false;
        _logger.LogDebug("Color picker cancelled");
    }

    private async Task<Color?> StartColorPickerAsync()
    {
        _isActive = true;
        _logger.LogDebug("Starting color picker");

        try
        {
            while (_isActive)
            {
                if (_keyboardService.IsKeyPressed("Escape"))
                {
                    _logger.LogDebug("Color picker cancelled by ESC key");
                    return null;
                }

                if (_keyboardService.IsKeyPressed("LeftButton"))
                {
                    var color = GetColorAtCurrentMousePosition();
                    _logger.LogDebug("Color picked: {Color}", color);
                    return color;
                }

                await Task.Delay(50);
            }
        }
        finally
        {
            _isActive = false;
        }

        return null;
    }

    private async Task<Color?> WaitForRightClickColorAsync()
    {
        _logger.LogDebug("Waiting for right click to pick color");
        
        // 右クリック待機のロジックを実装
        // この実装は簡易版です
        await Task.Delay(100);
        return GetColorAtCurrentMousePosition();
    }
}