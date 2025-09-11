using System.Drawing;
using System.IO;
using AutoTool.Services.Abstractions;
using AutoTool.Services.ObjectDetection;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace AutoTool.Services.Implementations;

/// <summary>
/// YOLO物体検出サービスの実装
/// </summary>
public class ObjectDetectionService : IObjectDetectionService
{
    private readonly ILogger<ObjectDetectionService> _logger;
    private bool _isInitialized = false;

    public ObjectDetectionService(ILogger<ObjectDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsModelInitialized => _isInitialized;

    public void InitializeModel(string onnxPath, int inputSize = 640, bool useDirectML = false, string[]? labels = null)
    {
        try
        {
            _logger.LogInformation("YOLOモデルを初期化中: {OnnxPath}, InputSize: {InputSize}, DirectML: {UseDirectML}", 
                onnxPath, inputSize, useDirectML);

            YoloWin.Init(onnxPath, inputSize, useDirectML, labels);
            _isInitialized = true;

            _logger.LogInformation("YOLOモデルの初期化が完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YOLOモデルの初期化に失敗しました: {OnnxPath}", onnxPath);
            _isInitialized = false;
            throw;
        }
    }

    public async Task<DetectionResult> DetectFromWindowAsync(string windowTitle, float confidenceThreshold = 0.45f, float iouThreshold = 0.15f, bool drawResults = true)
    {
        ThrowIfNotInitialized();

        return await Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("ウィンドウから物体検出を実行: {WindowTitle}", windowTitle);
                var result = YoloWin.DetectFromWindowTitle(windowTitle, confidenceThreshold, iouThreshold, drawResults);
                _logger.LogDebug("検出結果: {DetectionCount}個のオブジェクトを検出", result.Detections.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウからの物体検出に失敗: {WindowTitle}", windowTitle);
                throw;
            }
        });
    }

    public async Task<DetectionResult> DetectFromBitmapAsync(Bitmap bitmap, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f, bool drawResults = true)
    {
        ThrowIfNotInitialized();

        return await Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("Bitmapから物体検出を実行: {Width}x{Height}", bitmap.Width, bitmap.Height);
                var result = YoloWin.DetectFromBitmap(bitmap, confidenceThreshold, iouThreshold, drawResults);
                _logger.LogDebug("検出結果: {DetectionCount}個のオブジェクトを検出", result.Detections.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bitmapからの物体検出に失敗");
                throw;
            }
        });
    }

    public async Task<DetectionResult> DetectFromFileAsync(string imagePath, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f, bool drawResults = true)
    {
        ThrowIfNotInitialized();

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"画像ファイルが見つかりません: {imagePath}");
        }

        return await Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("ファイルから物体検出を実行: {ImagePath}", imagePath);
                
                using var mat = Cv2.ImRead(imagePath);
                if (mat.Empty())
                {
                    throw new InvalidOperationException($"画像の読み込みに失敗しました: {imagePath}");
                }

                var result = YoloWin.DetectFromMat(mat, confidenceThreshold, iouThreshold, drawResults);
                _logger.LogDebug("検出結果: {DetectionCount}個のオブジェクトを検出", result.Detections.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイルからの物体検出に失敗: {ImagePath}", imagePath);
                throw;
            }
        });
    }

    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("YOLOモデルが初期化されていません。InitializeModel()を先に呼び出してください。");
        }
    }
}