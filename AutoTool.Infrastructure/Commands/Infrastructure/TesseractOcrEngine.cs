using System.IO;
using OpenCvSharp;
using Tesseract;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

public sealed class TesseractOcrEngine : IOcrEngine
{
    public Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Width <= 0) throw new ArgumentOutOfRangeException(nameof(request.Width));
        if (request.Height <= 0) throw new ArgumentOutOfRangeException(nameof(request.Height));

        using var captured = CaptureTarget(request.WindowTitle, request.WindowClassName);
        using var roi = CreateRoi(captured, request.X, request.Y, request.Width, request.Height);
        using var processed = Preprocess(roi, request.PreprocessMode);

        var tempImagePath = Path.Combine(Path.GetTempPath(), $"autotool_ocr_{Guid.NewGuid():N}.png");

        try
        {
            Cv2.ImWrite(tempImagePath, processed);

            var tessdataPath = ResolveTessdataPath(request.TessdataPath);
            var psm = ParsePageSegMode(request.PageSegmentationMode);
            var language = string.IsNullOrWhiteSpace(request.Language) ? "jpn" : request.Language;

            using var engine = new TesseractEngine(tessdataPath, language, EngineMode.Default);
            engine.DefaultPageSegMode = psm;

            if (!string.IsNullOrWhiteSpace(request.Whitelist))
            {
                engine.SetVariable("tessedit_char_whitelist", request.Whitelist);
            }

            using var pix = Pix.LoadFromFile(tempImagePath);
            using var page = engine.Process(pix);

            var text = (page.GetText() ?? string.Empty).Trim();
            var confidence = Math.Clamp(page.GetMeanConfidence() * 100.0, 0.0, 100.0);

            return Task.FromResult(new OcrExtractionResult(text, confidence));
        }
        finally
        {
            TryDelete(tempImagePath);
        }
    }

    private static Mat CaptureTarget(string? windowTitle, string? windowClassName)
    {
        if (string.IsNullOrWhiteSpace(windowTitle) && string.IsNullOrWhiteSpace(windowClassName))
        {
            return Win32ScreenCaptureHelper.CaptureScreen();
        }

        return Win32ScreenCaptureHelper.CaptureWindow(windowTitle ?? string.Empty, windowClassName ?? string.Empty);
    }

    private static Mat CreateRoi(Mat captured, int x, int y, int width, int height)
    {
        var left = Math.Clamp(x, 0, captured.Width - 1);
        var top = Math.Clamp(y, 0, captured.Height - 1);
        var right = Math.Clamp(x + width, 0, captured.Width);
        var bottom = Math.Clamp(y + height, 0, captured.Height);

        if (right <= left || bottom <= top)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "OCRキャプチャ範囲が画像の外側です。");
        }

        return new Mat(captured, new OpenCvSharp.Rect(left, top, right - left, bottom - top));
    }

    private static Mat Preprocess(Mat source, string? preprocessMode)
    {
        var mode = (preprocessMode ?? "Gray").Trim();

        if (mode.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return source.Clone();
        }

        using var gray = ToGray(source);
        var output = new Mat();

        if (mode.Equals("Binarize", StringComparison.OrdinalIgnoreCase))
        {
            Cv2.Threshold(gray, output, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            return output;
        }

        if (mode.Equals("AdaptiveThreshold", StringComparison.OrdinalIgnoreCase))
        {
            Cv2.AdaptiveThreshold(gray, output, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 5);
            return output;
        }

        return gray.Clone();
    }

    private static Mat ToGray(Mat source)
    {
        var gray = new Mat();
        if (source.Channels() == 4)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);
            return gray;
        }

        if (source.Channels() == 3)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        return source.Clone();
    }

    private static string ResolveTessdataPath(string? tessdataPath)
    {
        if (!string.IsNullOrWhiteSpace(tessdataPath) && Directory.Exists(tessdataPath))
        {
            return tessdataPath;
        }

        var candidate = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        throw new DirectoryNotFoundException(
            "tessdata ディレクトリが見つかりません。GetVariable_OCR の tessdata 設定で有効なフォルダを指定してください。");
    }

    private static PageSegMode ParsePageSegMode(string? pageSegmentationMode)
    {
        return pageSegmentationMode switch
        {
            "7" => PageSegMode.SingleLine,
            "11" => PageSegMode.SparseText,
            "12" => PageSegMode.SparseTextOsd,
            "13" => PageSegMode.RawLine,
            _ => PageSegMode.SingleBlock
        };
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignore cleanup errors
        }
    }
}
