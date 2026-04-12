using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
//using OpenCvSharp.Extensions;
using System;
using System.Drawing;


namespace YoloWinLib;


/// <summary>
/// ウィンドウキャプチャ + YOLOv8 物体検出の簡易静的ファサード。
/// 1) <see cref="Init"/> でモデルをロード。
/// 2) <see cref="DetectFromWindowTitle"/> / <see cref="DetectFromBitmap"/> / <see cref="DetectFromMat"/> で推論。
/// </summary>
public static class YoloWin
{
    private static YoloV8Detector? _detector;


    /// <summary>YOLOv8 モデルをロードします。複数回呼ぶと前のセッションは破棄されます。</summary>
    public static void Init(string onnxPath, int inputSize = 640, bool useDirectML = false, string[]? labels = null)
    {
        _detector?.Dispose();
        _detector = new YoloV8Detector(onnxPath, inputSize, useDirectML) { Labels = labels };
    }


    /// <summary>
    /// 指定タイトルのウィンドウをキャプチャして検出します。
    /// タイトルが空の場合はデスクトップ全体をキャプチャします。
    /// </summary>
    public static DetectionResult DetectFromWindowTitle(string? windowTitle, float confTh = 0.45f, float iouTh = 0.15f, bool draw = true)
    {
        EnsureReady();
        DpiUtil.TryEnablePerMonitorDpi();
        
        Bitmap bmp;
        if (string.IsNullOrEmpty(windowTitle))
        {
            // グローバル（全画面）対象
            bmp = WindowCapturer.CaptureScreen();
        }
        else
        {
            // 指定ウィンドウ対象
            bmp = WindowCapturer.CaptureByTitle(windowTitle)
                ?? throw new InvalidOperationException($"ウィンドウのキャプチャに失敗しました: '{windowTitle}'");
        }
        
        using (bmp)
        {
            using var mat = BitmapConverter.ToMat(bmp);
            return DetectFromMat(mat, confTh, iouTh, draw);
        }
    }


    /// <summary>Bitmap から検出します。</summary>
    public static DetectionResult DetectFromBitmap(Bitmap bitmap, float confTh = 0.25f, float iouTh = 0.45f, bool draw = true)
    {
        EnsureReady();
        using var mat = BitmapConverter.ToMat(bitmap);
        return DetectFromMat(mat, confTh, iouTh, draw);
    }


    /// <summary>OpenCvSharp の Mat から検出します。</summary>
    public static DetectionResult DetectFromMat(Mat bgr, float confTh = 0.25f, float iouTh = 0.45f, bool draw = true)
    {
        EnsureReady();
        var dets = _detector!.Detect(bgr, confTh, iouTh);
        Mat? annotated = null;
        if (draw)
        {
            annotated = bgr.Clone();
            YoloDrawer.Draw(annotated, dets, _detector.Labels);
        }
        return new DetectionResult(dets, annotated);
    }


    private static void EnsureReady()
    {
        if (_detector is null) throw new InvalidOperationException("YoloWin.Init() を先に呼んでください");
    }
}

// もし BitmapConverter が使えない場合は、以下のような変換メソッドを追加してください。

public static class BitmapConverter
{
    public static Mat ToMat(System.Drawing.Bitmap bitmap)
    {
        // Windows API依存のため、プラットフォームチェックを追加
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("BitmapからMatへの変換はWindowsのみサポートされています。");

        var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
        try
        {
            var matType = bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb
                ? MatType.CV_8UC3
                : MatType.CV_8UC4;
            // 非推奨コンストラクタの代わりに FromPixelData を使用
            var mat = Mat.FromPixelData(bitmap.Height, bitmap.Width, matType, bmpData.Scan0, bmpData.Stride);
            return mat.Clone(); // メモリ解放のため Clone
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }
}

/// <summary>検出結果 + (任意) 描画済み画像。</summary>
public sealed record DetectionResult(IReadOnlyList<Detection> Detections, Mat? AnnotatedBgr)
{
    /// <summary>描画済み画像を保存します（<paramref name="path"/> が .png / .jpg など）。</summary>
    public void SaveAnnotated(string path)
    {
        if (AnnotatedBgr is null) throw new InvalidOperationException("draw=false で呼び出されています");
        Cv2.ImWrite(path, AnnotatedBgr);
    }
}