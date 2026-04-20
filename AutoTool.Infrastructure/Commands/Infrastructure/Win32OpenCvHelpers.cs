using System.Drawing;
using System.IO;
using AutoTool.Commands.Model.Input;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// 低レベル API 呼び出しをラップして共通化し、呼び出し側の実装を簡潔にします。
/// </summary>
public static class Win32ScreenCaptureHelper
{
    public static Mat CaptureScreen()
    {
        using var bitmap = Win32CaptureCore.CapturePrimaryScreen();
        return BitmapConverter.ToMat(bitmap);
    }

    public static Mat CaptureWindow(string windowTitle, string windowClassName = "")
    {
        using var bitmap = Win32CaptureCore.CaptureWindow(windowTitle, windowClassName);
        return BitmapConverter.ToMat(bitmap);
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

    public static Mat CaptureRegion(double x, double y, double width, double height)
    {
        using var bitmap = Win32CaptureCore.CaptureDesktopRegion(
            new Rectangle((int)x, (int)y, (int)width, (int)height));
        return BitmapConverter.ToMat(bitmap);
    }
}

/// <summary>
/// 低レベル API 呼び出しをラップして共通化し、呼び出し側の実装を簡潔にします。
/// </summary>
internal static class OpenCvImageSearchHelper
{
    public static Task<OpenCvSharp.Point?> SearchImageAsync(
        string imagePath,
        CancellationToken token,
        double threshold = 0.8,
        CommandColor? searchColor = null,
        string windowTitle = "",
        string windowClassName = "")
    {
        return Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return (OpenCvSharp.Point?)null;
            }

            token.ThrowIfCancellationRequested();
            using var targetMat = string.IsNullOrWhiteSpace(windowTitle) && string.IsNullOrWhiteSpace(windowClassName)
                ? Win32ScreenCaptureHelper.CaptureScreen()
                : Win32ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);

            token.ThrowIfCancellationRequested();
            using var templateMat = new Mat(imagePath);
            token.ThrowIfCancellationRequested();

            if (searchColor is null)
            {
                EnsureGrayInPlace(targetMat);
                EnsureGrayInPlace(templateMat);
            }
            else
            {
                EnsureBgrInPlace(targetMat);
                EnsureBgrInPlace(templateMat);

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

            token.ThrowIfCancellationRequested();
            using var result = new Mat();
            Cv2.MatchTemplate(targetMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            token.ThrowIfCancellationRequested();
            Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal < threshold)
            {
                return (OpenCvSharp.Point?)null;
            }

            return new OpenCvSharp.Point(maxLoc.X + templateMat.Width / 2, maxLoc.Y + templateMat.Height / 2);
        }, token);
    }

    private static void EnsureGrayInPlace(Mat mat)
    {
        Action<Mat> convert = mat.Channels() switch
        {
            1 => static (Mat _) => { },
            3 => static (Mat m) => Cv2.CvtColor(m, m, ColorConversionCodes.BGR2GRAY),
            4 => static (Mat m) => Cv2.CvtColor(m, m, ColorConversionCodes.BGRA2GRAY),
            _ => static (Mat m) => throw new InvalidOperationException($"グレースケール変換で未対応のチャネル数です: {m.Channels()}")
        };

        convert(mat);
    }

    private static void EnsureBgrInPlace(Mat mat)
    {
        Action<Mat> convert = mat.Channels() switch
        {
            3 => static (Mat _) => { },
            4 => static (Mat m) => Cv2.CvtColor(m, m, ColorConversionCodes.BGRA2BGR),
            1 => static (Mat m) => Cv2.CvtColor(m, m, ColorConversionCodes.GRAY2BGR),
            _ => static (Mat m) => throw new InvalidOperationException($"BGR変換で未対応のチャネル数です: {m.Channels()}")
        };

        convert(mat);
    }
}
