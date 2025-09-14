using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Abstractions;
using System.IO;
using System.Text;

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

    public async Task<Bitmap> CaptureWindowAsync(string windowTitle, string windowClass, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Capturing window: Title='{WindowTitle}', Class='{WindowClass}'", windowTitle, windowClass);

            cancellationToken.ThrowIfCancellationRequested();

            IntPtr hwnd = IntPtr.Zero;

            // Try direct FindWindow if possible
            if (!string.IsNullOrEmpty(windowClass) || !string.IsNullOrEmpty(windowTitle))
            {
                // pass null if empty so FindWindow can match by the other parameter
                var cls = string.IsNullOrEmpty(windowClass) ? null : windowClass;
                var title = string.IsNullOrEmpty(windowTitle) ? null : windowTitle;
                try
                {
                    hwnd = FindWindow(cls, title);
                }
                catch
                {
                    hwnd = IntPtr.Zero;
                }
            }

            // If not found, enumerate windows and try partial match on title/class
            if (hwnd == IntPtr.Zero)
            {
                void Callback(IntPtr hWnd)
                {
                    if (hwnd != IntPtr.Zero) return; // already found
                    if (cancellationToken.IsCancellationRequested) return;

                    var len = GetWindowTextLength(hWnd);
                    var sb = new StringBuilder(len + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    var title = sb.ToString();

                    var classSb = new StringBuilder(256);
                    GetClassName(hWnd, classSb, classSb.Capacity);
                    var cls = classSb.ToString();

                    var titleMatch = string.IsNullOrEmpty(windowTitle) || title.IndexOf(windowTitle, StringComparison.OrdinalIgnoreCase) >= 0;
                    var classMatch = string.IsNullOrEmpty(windowClass) || cls.IndexOf(windowClass, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (titleMatch && classMatch)
                    {
                        hwnd = hWnd;
                    }
                }

                EnumWindows((h, l) =>
                {
                    Callback(h);
                    // stop when found or cancelled
                    return hwnd == IntPtr.Zero && !cancellationToken.IsCancellationRequested;
                }, IntPtr.Zero);
            }

            if (hwnd == IntPtr.Zero)
            {
                _logger.LogWarning("Window not found: Title='{WindowTitle}', Class='{WindowClass}'", windowTitle, windowClass);
                throw new InvalidOperationException($"Window not found: Title='{windowTitle}', Class='{windowClass}'");
            }

            // Get client rect and convert to screen coordinates
            if (!GetClientRect(hwnd, out RECT clientRect))
            {
                // fallback to window rect if client rect fails
                GetWindowRect(hwnd, out clientRect);
            }

            var topLeft = new POINT { x = clientRect.Left, y = clientRect.Top };
            if (!ClientToScreen(hwnd, ref topLeft))
            {
                // If ClientToScreen fails, try using window rect top-left
                GetWindowRect(hwnd, out var winRect);
                topLeft.x = winRect.Left;
                topLeft.y = winRect.Top;
            }

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;
            if (width <= 0 || height <= 0)
            {
                _logger.LogWarning("Window has non-positive size: {Width}x{Height}", width, height);
                throw new InvalidOperationException("Window has non-positive size.");
            }

            // Try PrintWindow first (captures even if occluded on many apps)
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            bool printed = false;

            try
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    IntPtr hdc = g.GetHdc();
                    try
                    {
                        printed = PrintWindow(hwnd, hdc, 0);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "PrintWindow threw an exception; will fallback to CopyFromScreen");
                        printed = false;
                    }
                    finally
                    {
                        try { g.ReleaseHdc(hdc); } catch { }
                    }
                }

                if (!printed)
                {
                    // Fallback: copy from screen (requires window to be visible)
                    using var g2 = Graphics.FromImage(bitmap);
                    g2.CopyFromScreen(topLeft.x, topLeft.y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch
            {
                // Dispose bitmap on error
                bitmap.Dispose();
                throw;
            }

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

    private async Task<Point?> FindImageInWindowAsync(string windowTitle, string windowClass, Bitmap targetImage, double threshold = 0.9, CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            _logger.LogDebug("Finding image in window: Title='{WindowTitle}', Class='{WindowClass}', Threshold={Threshold}", windowTitle, windowClass, threshold);
            using var windowBitmap = await CaptureWindowAsync(windowTitle, windowClass, cancellationToken);
            return FindImageInBitmap(windowBitmap, targetImage, threshold);
        }, cancellationToken);
    }

    private async Task<Point?> FindImageAsync(Bitmap targetImage, double threshold = 0.9, CancellationToken cancellationToken = default)
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

    public async Task<Point?> FindImageAsync(string imagePath, string windowTitle = "", string windowClass = "", double threshold = 0.9, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("Image file not found", imagePath);
        }
        using var targetImage = new Bitmap(imagePath);
        if (!string.IsNullOrEmpty(windowTitle) || !string.IsNullOrEmpty(windowClass))
        {
            return await FindImageInWindowAsync(windowTitle, windowClass, targetImage, threshold, cancellationToken);
        }
        else
        {
            return await FindImageAsync(targetImage, threshold, cancellationToken);
        }
    }

    public async Task<Point[]> FindAllImagesAsync(string imagePath, string windowTitle = "", string windowClass = "", double threshold = 0.9, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("Image file not found", imagePath);
        }
        using var targetImage = new Bitmap(imagePath);
        if (!string.IsNullOrEmpty(windowTitle) || !string.IsNullOrEmpty(windowClass))
        {
            throw new NotImplementedException("Finding all images in a specific window is not implemented.");
        }
        else
        {
            return await FindAllImagesAsync(targetImage, threshold, cancellationToken);
        }
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

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion
}