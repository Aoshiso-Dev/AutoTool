using System.Drawing;
using System.IO;
using System.Linq;
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

    public static Task<IReadOnlyList<OpenCvSharp.Point>> SearchImagesAsync(
        string imagePath,
        CancellationToken token,
        double threshold = 0.8,
        CommandColor? searchColor = null,
        string windowTitle = "",
        string windowClassName = "",
        int maxResults = 20)
    {
        return Task.Run<IReadOnlyList<OpenCvSharp.Point>>(() =>
        {
            token.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return [];
            }

            var safeMaxResults = Math.Max(1, maxResults);

            token.ThrowIfCancellationRequested();
            using var targetMat = string.IsNullOrWhiteSpace(windowTitle) && string.IsNullOrWhiteSpace(windowClassName)
                ? Win32ScreenCaptureHelper.CaptureScreen()
                : Win32ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);

            token.ThrowIfCancellationRequested();
            using var templateMat = new Mat(imagePath);
            token.ThrowIfCancellationRequested();

            if (targetMat.Width < templateMat.Width || targetMat.Height < templateMat.Height)
            {
                return [];
            }

            PrepareSearchMats(targetMat, templateMat, searchColor);

            token.ThrowIfCancellationRequested();
            using var result = new Mat();
            Cv2.MatchTemplate(targetMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            token.ThrowIfCancellationRequested();

            var candidates = CollectCandidates(result, threshold, templateMat.Width, templateMat.Height, safeMaxResults * 10, token);
            if (candidates.Count == 0)
            {
                return [];
            }

            return SelectByNms(candidates, safeMaxResults, 0.3)
                .Select(static x => new OpenCvSharp.Point(x.CenterX, x.CenterY))
                .ToArray();
        }, token);
    }

    private static void PrepareSearchMats(Mat targetMat, Mat templateMat, CommandColor? searchColor)
    {
        if (searchColor is null)
        {
            EnsureGrayInPlace(targetMat);
            EnsureGrayInPlace(templateMat);
            return;
        }

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

    private static List<ImageMatchCandidate> CollectCandidates(
        Mat result,
        double threshold,
        int templateWidth,
        int templateHeight,
        int maxCandidates,
        CancellationToken token)
    {
        var candidates = new List<ImageMatchCandidate>();
        for (var y = 0; y < result.Rows; y++)
        {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < result.Cols; x++)
            {
                var score = result.At<float>(y, x);
                if (score < threshold)
                {
                    continue;
                }

                candidates.Add(new ImageMatchCandidate(x, y, templateWidth, templateHeight, score));
            }
        }

        if (candidates.Count == 0)
        {
            return candidates;
        }

        return candidates
            .OrderByDescending(static c => c.Score)
            .Take(Math.Max(1, maxCandidates))
            .ToList();
    }

    private static IEnumerable<ImageMatchCandidate> SelectByNms(
        IReadOnlyList<ImageMatchCandidate> candidates,
        int maxResults,
        double iouThreshold)
    {
        var selected = new List<ImageMatchCandidate>(Math.Min(maxResults, candidates.Count));
        foreach (var candidate in candidates)
        {
            if (selected.Count >= maxResults)
            {
                break;
            }

            if (selected.Any(existing => ComputeIoU(existing, candidate) >= iouThreshold))
            {
                continue;
            }

            selected.Add(candidate);
        }

        return selected;
    }

    private static double ComputeIoU(ImageMatchCandidate a, ImageMatchCandidate b)
    {
        var interLeft = Math.Max(a.Left, b.Left);
        var interTop = Math.Max(a.Top, b.Top);
        var interRight = Math.Min(a.Right, b.Right);
        var interBottom = Math.Min(a.Bottom, b.Bottom);

        var interWidth = Math.Max(0, interRight - interLeft);
        var interHeight = Math.Max(0, interBottom - interTop);
        var interArea = interWidth * interHeight;
        if (interArea <= 0)
        {
            return 0;
        }

        var unionArea = a.Area + b.Area - interArea;
        return unionArea <= 0 ? 0 : interArea / unionArea;
    }

    private readonly record struct ImageMatchCandidate(int Left, int Top, int Width, int Height, double Score)
    {
        public int Right => Left + Width;
        public int Bottom => Top + Height;
        public int Area => Width * Height;
        public int CenterX => Left + (Width / 2);
        public int CenterY => Top + (Height / 2);
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

