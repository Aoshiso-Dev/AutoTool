using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Abstractions;

namespace AutoTool.Services.Implementations;

/// <summary>
/// 画面キャプチャサービスの実装（責任分離後）
/// </summary>
public class CaptureService : ICaptureService
{
    private readonly ILogger<CaptureService> _logger;

    public CaptureService(ILogger<CaptureService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Bitmap> CaptureScreenAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Capturing full screen");
            var screenBounds = GetScreenBounds();
            return CaptureRegion(screenBounds);
        }, cancellationToken);
    }

    public async Task<Bitmap> CaptureRegionAsync(Rectangle region, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Capturing region: {Region}", region);
            return CaptureRegion(region);
        }, cancellationToken);
    }

    public Color GetColorAt(int x, int y)
    {
        _logger.LogDebug("Getting color at ({X}, {Y})", x, y);
        
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Size(1, 1));
        
        return bitmap.GetPixel(0, 0);
    }

    public Color GetColorAtMousePosition()
    {
        var cursorPos = GetCurrentMousePosition();
        return GetColorAt(cursorPos.X, cursorPos.Y);
    }

    public async Task<Color?> ShowColorPickerAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            _logger.LogDebug("Starting color picker");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsKeyPressed(VK_ESCAPE))
                {
                    _logger.LogDebug("Color picker cancelled");
                    return (Color?)null;
                }
                
                if (IsKeyPressed(VK_LBUTTON))
                {
                    var color = GetColorAtMousePosition();
                    _logger.LogDebug("Color picked: {Color}", color);
                    return (Color?)color;
                }
                
                await Task.Delay(50, cancellationToken);
            }
            
            return (Color?)null;
        }, cancellationToken);
    }

    public async Task<Point?> FindImageAsync(Bitmap targetImage, double threshold = 0.9, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Finding image on screen with threshold {Threshold}", threshold);
            
            using var screenBitmap = CaptureScreen();
            return FindImageInBitmap(screenBitmap, targetImage, threshold);
        }, cancellationToken);
    }

    public async Task<Point[]> FindAllImagesAsync(Bitmap targetImage, double threshold = 0.9, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Finding all images on screen with threshold {Threshold}", threshold);
            
            using var screenBitmap = CaptureScreen();
            return FindAllImagesInBitmap(screenBitmap, targetImage, threshold);
        }, cancellationToken);
    }

    #region Private Methods

    private Bitmap CaptureRegion(Rectangle region)
    {
        var bitmap = new Bitmap(region.Width, region.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.Location, Point.Empty, region.Size);
        return bitmap;
    }

    private Bitmap CaptureScreen()
    {
        var bounds = GetScreenBounds();
        return CaptureRegion(bounds);
    }

    private Rectangle GetScreenBounds()
    {
        return new Rectangle(0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
    }

    private Point GetCurrentMousePosition()
    {
        GetCursorPos(out var point);
        return new Point(point.x, point.y);
    }

    private Point? FindImageInBitmap(Bitmap source, Bitmap target, double threshold)
    {
        for (int x = 0; x <= source.Width - target.Width; x++)
        {
            for (int y = 0; y <= source.Height - target.Height; y++)
            {
                if (IsImageMatch(source, target, x, y, threshold))
                {
                    return new Point(x, y);
                }
            }
        }
        return null;
    }

    private Point[] FindAllImagesInBitmap(Bitmap source, Bitmap target, double threshold)
    {
        var matches = new List<Point>();
        
        for (int x = 0; x <= source.Width - target.Width; x++)
        {
            for (int y = 0; y <= source.Height - target.Height; y++)
            {
                if (IsImageMatch(source, target, x, y, threshold))
                {
                    matches.Add(new Point(x, y));
                }
            }
        }
        
        return matches.ToArray();
    }

    private bool IsImageMatch(Bitmap source, Bitmap target, int offsetX, int offsetY, double threshold)
    {
        int totalPixels = target.Width * target.Height;
        int matchingPixels = 0;
        
        for (int x = 0; x < target.Width; x++)
        {
            for (int y = 0; y < target.Height; y++)
            {
                var sourceColor = source.GetPixel(offsetX + x, offsetY + y);
                var targetColor = target.GetPixel(x, y);
                
                if (ColorsAreClose(sourceColor, targetColor, 30))
                {
                    matchingPixels++;
                }
            }
        }
        
        return (double)matchingPixels / totalPixels >= threshold;
    }

    private bool ColorsAreClose(Color color1, Color color2, int tolerance)
    {
        return Math.Abs(color1.R - color2.R) <= tolerance &&
               Math.Abs(color1.G - color2.G) <= tolerance &&
               Math.Abs(color1.B - color2.B) <= tolerance;
    }

    #endregion

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private bool IsKeyPressed(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }

    private const int VK_ESCAPE = 0x1B;
    private const int VK_LBUTTON = 0x01;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    #endregion
}