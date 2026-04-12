using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Color = System.Windows.Media.Color;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutoTool.Commands.Infrastructure;

public static class Win32ScreenCaptureHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, CopyPixelOperation rop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiObj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

    public static Mat CaptureScreen()
    {
        var width = (int)SystemParameters.VirtualScreenWidth;
        var height = (int)SystemParameters.VirtualScreenHeight;

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        return BitmapConverter.ToMat(bitmap);
    }

    public static Mat CaptureWindow(string windowTitle, string windowClassName = "")
    {
        var hWnd = FindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!GetWindowRect(hWnd, out var rect))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        var hdcSrc = GetWindowDC(hWnd);
        var hdcDest = CreateCompatibleDC(hdcSrc);
        var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
        var hOld = SelectObject(hdcDest, hBitmap);

        try
        {
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
            using var bitmap = Image.FromHbitmap(hBitmap);
            return BitmapConverter.ToMat((Bitmap)bitmap);
        }
        finally
        {
            SelectObject(hdcDest, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            ReleaseDC(hWnd, hdcSrc);
        }
    }

    public static void SaveCapture(Mat image, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        image.SaveImage(filePath);
    }

    public static Mat CaptureRegion(System.Windows.Rect region)
    {
        using var bitmap = new Bitmap((int)region.Width, (int)region.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen((int)region.X, (int)region.Y, 0, 0, bitmap.Size);
        return BitmapConverter.ToMat(bitmap);
    }
}

internal static class OpenCvImageSearchHelper
{
    public static Task<OpenCvSharp.Point?> SearchImageAsync(
        string imagePath,
        CancellationToken token,
        double threshold = 0.8,
        Color? searchColor = null,
        string windowTitle = "",
        string windowClassName = "")
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return (OpenCvSharp.Point?)null;
            }

            using var targetMat = string.IsNullOrWhiteSpace(windowTitle) && string.IsNullOrWhiteSpace(windowClassName)
                ? Win32ScreenCaptureHelper.CaptureScreen()
                : Win32ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);

            using var templateMat = new Mat(imagePath);

            if (searchColor is null)
            {
                Cv2.CvtColor(targetMat, targetMat, ColorConversionCodes.BGRA2GRAY);
                Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2GRAY);
            }
            else
            {
                Cv2.CvtColor(targetMat, targetMat, ColorConversionCodes.BGRA2BGR);
                Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2BGR);

                var lower = new Scalar(
                    Math.Max(searchColor.Value.R - 20, 0),
                    Math.Max(searchColor.Value.G - 20, 0),
                    Math.Max(searchColor.Value.B - 20, 0));

                var upper = new Scalar(
                    Math.Min(searchColor.Value.R + 20, 255),
                    Math.Min(searchColor.Value.G + 20, 255),
                    Math.Min(searchColor.Value.B + 20, 255));

                using var targetMask = new Mat();
                Cv2.InRange(targetMat, lower, upper, targetMask);
                Cv2.BitwiseAnd(targetMat, targetMat, targetMat, targetMask);

                using var templateMask = new Mat();
                Cv2.InRange(templateMat, lower, upper, templateMask);
                Cv2.BitwiseAnd(templateMat, templateMat, templateMat, templateMask);
            }

            using var result = new Mat();
            Cv2.MatchTemplate(targetMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal < threshold)
            {
                return (OpenCvSharp.Point?)null;
            }

            return new OpenCvSharp.Point(maxLoc.X + templateMat.Width / 2, maxLoc.Y + templateMat.Height / 2);
        }, token);
    }
}

