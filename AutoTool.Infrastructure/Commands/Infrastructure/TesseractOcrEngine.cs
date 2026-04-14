using System.Diagnostics;
using System.Globalization;
using System.IO;
using OpenCvSharp;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

public sealed class TesseractOcrEngine : IOcrEngine
{
    public async Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Width <= 0) throw new ArgumentOutOfRangeException(nameof(request.Width));
        if (request.Height <= 0) throw new ArgumentOutOfRangeException(nameof(request.Height));

        var tempBase = Path.Combine(Path.GetTempPath(), $"autotool_ocr_{Guid.NewGuid():N}");
        var imagePath = tempBase + ".png";
        var outputBase = tempBase + "_out";
        var txtPath = outputBase + ".txt";
        var tsvPath = outputBase + ".tsv";

        try
        {
            using var captured = CaptureTarget(request.WindowTitle, request.WindowClassName);
            using var roi = CreateRoi(captured, request.X, request.Y, request.Width, request.Height);
            using var processed = Preprocess(roi, request.PreprocessMode);

            Cv2.ImWrite(imagePath, processed);

            await RunTesseractAsync(request, imagePath, outputBase, cancellationToken);

            var text = File.Exists(txtPath)
                ? (await File.ReadAllTextAsync(txtPath, cancellationToken)).Trim()
                : string.Empty;

            var confidence = File.Exists(tsvPath)
                ? ParseConfidence(await File.ReadAllTextAsync(tsvPath, cancellationToken))
                : 0.0;

            return new OcrExtractionResult(text, confidence);
        }
        finally
        {
            TryDelete(imagePath);
            TryDelete(txtPath);
            TryDelete(tsvPath);
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
            throw new ArgumentOutOfRangeException(nameof(width), "OCR領域がキャプチャ範囲外です。");
        }

        return new Mat(captured, new Rect(left, top, right - left, bottom - top));
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

    private static async Task RunTesseractAsync(
        OcrRequest request,
        string imagePath,
        string outputBase,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = string.IsNullOrWhiteSpace(request.TesseractPath) ? "tesseract" : request.TesseractPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add(imagePath);
        psi.ArgumentList.Add(outputBase);

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            psi.ArgumentList.Add("-l");
            psi.ArgumentList.Add(request.Language);
        }

        if (!string.IsNullOrWhiteSpace(request.PageSegmentationMode))
        {
            psi.ArgumentList.Add("--psm");
            psi.ArgumentList.Add(request.PageSegmentationMode);
        }

        if (!string.IsNullOrWhiteSpace(request.TessdataPath))
        {
            psi.ArgumentList.Add("--tessdata-dir");
            psi.ArgumentList.Add(request.TessdataPath);
        }

        if (!string.IsNullOrWhiteSpace(request.Whitelist))
        {
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add($"tessedit_char_whitelist={request.Whitelist}");
        }

        psi.ArgumentList.Add("txt");
        psi.ArgumentList.Add("tsv");

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"tesseract 実行に失敗しました (exit={process.ExitCode}). {stderr} {stdout}".Trim());
        }
    }

    private static double ParseConfidence(string tsvContent)
    {
        if (string.IsNullOrWhiteSpace(tsvContent))
        {
            return 0.0;
        }

        var lines = tsvContent
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1);

        var confidences = new List<double>();

        foreach (var line in lines)
        {
            var columns = line.Split('\t');
            if (columns.Length < 11)
            {
                continue;
            }

            if (!double.TryParse(columns[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var conf))
            {
                continue;
            }

            if (conf >= 0)
            {
                confidences.Add(conf);
            }
        }

        if (confidences.Count == 0)
        {
            return 0.0;
        }

        return confidences.Average();
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
            // no-op
        }
    }
}
